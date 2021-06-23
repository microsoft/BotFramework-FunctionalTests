# syntax=docker/dockerfile:1
FROM python:3.8-buster

# Set virtual environment
ENV VIRTUAL_ENV=/opt/venv
RUN python3 -m venv $VIRTUAL_ENV
ENV PATH="$VIRTUAL_ENV/bin:$PATH"

# Update pip
RUN python -m pip install --upgrade pip

# Copy Source Code into the container
WORKDIR /app
COPY . .

# Install dependencies
RUN pip install -r requirements.txt

# Run the application
CMD python app.py
