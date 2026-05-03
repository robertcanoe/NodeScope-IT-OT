FROM python:3.12-slim AS runtime

WORKDIR /app

COPY python/processor/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY python/processor/ ./

ENTRYPOINT ["python", "main.py"]
