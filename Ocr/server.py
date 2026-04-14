from contextlib import asynccontextmanager

from fastapi import FastAPI
import base64
from pydantic import BaseModel
import uvicorn

from captcha_ocr import initialize, ocr

class ApiPredictRequestModel(BaseModel):
    body: str


@asynccontextmanager
async def lifespan(app: FastAPI):
    initialize()
    yield


app = FastAPI(lifespan=lifespan)

@app.post("/captcha-ocr")
def api_image_ocr(postData: ApiPredictRequestModel):
    img_bytes = base64.b64decode(postData.body)
    return ocr(img_bytes=img_bytes)

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=3123)

#chạy bằng lệnh: python .\server.py
