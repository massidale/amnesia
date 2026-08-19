#!/usr/bin/env bash
# Il dominio non vive dentro Assets/: e' un progetto .NET a se', e i suoi test
# girano in mezzo secondo senza aprire l'editor. Unity lo consuma come una DLL
# netstandard2.1, che e' il formato che carica senza adattatori.
#
# Da rilanciare dopo ogni modifica al dominio. Non e' automatico apposta: un
# copia-e-incolla che parte da solo nasconde quale versione stai giocando.
set -e
cd "$(dirname "$0")"
dotnet build Amnesia.Domain/Amnesia.Domain.csproj -c Release --nologo -v quiet
cp Amnesia.Domain/bin/Release/netstandard2.1/Amnesia.Domain.dll Unity/Assets/Plugins/

# Il contenuto e' dati, non codice, e vive fuori da Assets/ per la stessa
# ragione del dominio: si modifica scrivendo, non aprendo un editor. Unity lo
# legge da StreamingAssets, che e' l'unica cartella che sopravvive intatta a una
# build senza passare per l'importatore.
mkdir -p Unity/Assets/StreamingAssets
rsync -a --delete content/ Unity/Assets/StreamingAssets/
echo "Amnesia.Domain.dll -> Unity/Assets/Plugins/"
echo "content/           -> Unity/Assets/StreamingAssets/"
