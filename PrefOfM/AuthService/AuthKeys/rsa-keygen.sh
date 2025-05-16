# Генерация приватного ключа
openssl genrsa -out private_key.pem 2048

# Извлечение публичного ключа
openssl rsa -in private_key.pem -pubout -out public_key.pem