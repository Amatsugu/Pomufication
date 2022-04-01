#!/bin/bash
cd /Sites/Pomufication/Pomufication
#git pull
sudo dotnet publish -c Release
cd bin/Release/net6.0/publish
sudo dotnet Pomufication.dll --enviroment "Production"