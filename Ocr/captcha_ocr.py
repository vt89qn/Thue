from pathlib import Path

import numpy as np
import tensorflow as tf

model_data = None
predictors = None
MODEL_PATH = Path(__file__).resolve().parent / "model" / "thue"


def initialize():
    gpus = tf.config.list_physical_devices('GPU')
    if gpus:
        for gpu in gpus:
            tf.config.experimental.set_memory_growth(gpu, True)
    model, _, _, _ = get_model()
    get_predictor(model)


def ocr(img_bytes=None, img_np=None):
    model, width, height, num_to_char = get_model()
    predict = get_predictor(model)

    img_array = preprocess(img_bytes, img_np, width, height)
    pred = predict(img_array)
    text = postprocess(pred, num_to_char)[0]

    return {"text": text.strip()}


def get_model():
    global model_data

    if model_data is None:
        if not MODEL_PATH.is_dir():
            raise ValueError("wrong [app]")

        model = tf.keras.models.load_model(str(MODEL_PATH))
        configs = model.get_layer(name="ctc_loss").get_config()

        model = tf.keras.models.Model(
            model.get_layer(name="image").input, model.get_layer(name="dense2").output
        )

        characters = np.array(configs["characters"])
        char_to_num = tf.keras.layers.StringLookup(
            vocabulary=list(characters), mask_token=None, oov_token=""
        )
        num_to_char = tf.keras.layers.StringLookup(
            vocabulary=char_to_num.get_vocabulary(),
            mask_token=None,
            invert=True,
            oov_token="",
        )

        model_data = [
            model,
            characters,
            int(configs["width"]),
            int(configs["height"]),
            num_to_char,
        ]

    model = model_data[0]
    characters = model_data[1]
    width = model_data[2]
    height = model_data[3]
    num_to_char = model_data[4]

    return model, width, height, num_to_char


def get_predictor(model):
    global predictors

    if predictors is None:
        @tf.function
        def compiled_predict(inputs):
            return model(inputs, training=False)

        predictors = compiled_predict

    return predictors


def preprocess(img_bytes, img_np, width, height):
    if img_np is None:
        img_array = tf.io.decode_png(img_bytes, channels=1)
    else:
        img_array = img_np
        if len(img_array.shape) == 2:
            img_array = np.expand_dims(img_array, axis=-1)
    img_array = tf.image.convert_image_dtype(img_array, tf.float32)
    img_array = tf.image.resize(img_array, [height, width])
    img_array = tf.keras.utils.img_to_array(img_array)
    img_array = tf.transpose(img_array, perm=[1, 0, 2])
    return tf.expand_dims(img_array, axis=0)


def postprocess(pred, num_to_char):
    input_len = np.ones(pred.shape[0]) * pred.shape[1]
    results = tf.keras.backend.ctc_decode(pred, input_length=input_len, greedy=True)[0][0]
    output_text = []
    for res in results:
        res = tf.strings.reduce_join(num_to_char(res)).numpy().decode("utf-8")
        output_text.append(res)
    return output_text
