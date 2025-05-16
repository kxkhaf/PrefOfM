# 🎧 Music Recommendation System Based on Human Emotional State

Music has a significant impact on a person's emotional state — this idea forms the core of this service. The system analyzes the user's voice using a neural network, determines their current emotional state, and selects suitable music. This makes interaction with the service intuitive, personalized, and emotionally meaningful.

---

## ⚙️ Technologies Used

* **Python** — training the neural network (CNN, export to ONNX)
* **ASP.NET (C#)** — backend logic
* **React** — user interface
* **PostgreSQL + EF** — database
* **Redis** — caching
* **Docker + Docker Compose** — microservice deployment
* **Nginx** — reverse proxy and static frontend server

---

## 🧠 Project Architecture

* **AuthService** — authentication and JWT authorization
* **EmailService** — email confirmation
* **MusicService** — API wrapper for the ONNX neural network
* **ClientApp** — React frontend served via Nginx
* **Nginx** — proxies API requests and serves React static files

---

## 📈 How It Works

1. The user records their voice via the interface
2. The audio is analyzed by the neural network (ONNX)
3. Based on the detected emotion, a personalized playlist is generated
4. Music is played along with a synchronized video stream

---

## 📬 Email Setup

To enable registration confirmation, you need to configure the SMTP server for **EmailService**.

Open the `appsettings.json` and `appsettings.Development.json` files in the `EmailService` project and specify your email settings:

```json
"SmtpSettings": {
  "Host": "smtp.mail.ru",
  "Port": 587,
  "User": "youremail@mail.ru",
  "Password": "your_password",
  "From": "youremail@mail.ru",
  "UseSsl": true,
  "TimeoutMilliseconds": 10000,
  "BaseUrl": "https://localhost"
}
```

---

## 💡 Running the Project

### 1. Build Docker Images

Navigate to the project root and run the following commands:

```bash
# Build backend services
docker build -t authservice    -f AuthService/Dockerfile    .
docker build -t emailservice   -f EmailService/Dockerfile   .
docker build -t musicservice   -f MusicService/Dockerfile   .

# Build frontend and embed it into Nginx
docker build -t clientapp      -f ClientApp/Dockerfile      ClientApp
```

### 2. Start the Containers

```bash
docker-compose up -d
```

### 3. Initialize the Database

1. Navigate to the `MusicService` folder
2. Copy the **SeedKey** from `appsettings.json`
3. Open the following URL in your browser:

```
https://localhost/database/create/seed
```

4. Enter the **SeedKey** in the **Seed Parameter** field and click **Seed Database**

---

## 🎨 Interface Features

* Registration with email confirmation
* Quick start with a random song
* Search by title, emotion, and artist
* Emotion panel and voice recognition
* Listening history and favorites
* Visual player with seeking and synchronized video

---

## 📊 Model Performance

The model classifies audio into 5 emotions:

* Positive
* Sad
* Neutral
* Angry
* Other

**Accuracy on the test set: 71.23%**