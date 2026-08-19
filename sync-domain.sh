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
echo "Amnesia.Domain.dll -> Unity/Assets/Plugins/"
