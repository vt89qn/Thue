from fastapi import FastAPI
import base64
from pydantic import BaseModel

from captcha_ocr import ocr

class ApiPredictRequestModel(BaseModel):
    body: str


# instantiate flask app
app = FastAPI()

@app.post("/captcha-ocr")
def api_image_ocr(postData: ApiPredictRequestModel):
    img_bytes = base64.b64decode(postData.body)
    return ocr(img_bytes=img_bytes)
