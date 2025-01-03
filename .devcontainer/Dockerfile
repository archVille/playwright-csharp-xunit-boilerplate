FROM mcr.microsoft.com/vscode/devcontainers/universal:latest

ENV APT_KEY_DONT_WARN_ON_DANGEROUS_USAGE=DontWarn

# Google Chrome installation
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | sudo apt-key add -
RUN echo 'deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main' | sudo tee /etc/apt/sources.list.d/google-chrome.list
RUN sudo apt-get update
RUN sudo apt-get --yes install google-chrome-stable 2>&1

# Firefox installation
RUN sudo apt-get --yes install firefox 2>&1
