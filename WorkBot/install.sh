echo "WorkBot: Установка сервиса"
sudo systemctl enable /usr/share/remeslo/WorkBot/WorkBot.service || true
hostName=$(hostname | tr '[:lower:]' '[:upper:]')
echo "WorkBot: сервер $hostName"
if [ -f  /usr/share/remeslo/WorkBot/appsettings."$hostName".json ]
then
  echo "WorkBot: Копирование конфигурации для сервера $hostName"
  sudo cp /usr/share/remeslo/WorkBot/appsettings."$hostName".json /usr/share/remeslo/WorkBot/appsettings.json
else
  echo "WorkBot: Копирование конфигурации по умолчанию"
  sudo cp /usr/share/remeslo/WorkBot/appsettings.DEFAULT.json /usr/share/remeslo/WorkBot/appsettings.json
fi
echo "WorkBot: Запуск сервиса"
sudo systemctl start WorkBot || true
echo "WorkBot: Настройка завершена"