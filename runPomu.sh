#!/bin/bash
cd /Sites/Pomufication/Pomufication
#git pull
sudo dotnet publish -c Release
cd /Sites/Pomufication/Pomufication/bin/Release/net9.0/publish
dotnet Pomufication.dll --enviroment "Production"
