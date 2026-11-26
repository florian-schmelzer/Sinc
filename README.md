# Application Updater

Der Bootloader liest die Dateiversion der lokal installierten SampleApplication. Wenn der UpdateService eine neuere Version anbietet, lädt der Bootloader diese Version herunter und tauscht die lokale EXE nach dem vollständigen Download aus. Anschließend startet er die SampleApplication aus ihrem Installationsverzeichnis und beendet sich.

## Voraussetzungen

- Windows mit PowerShell
- .NET 10 SDK. Die Demo wurde mit SDK `10.0.201` geprüft.
- Die Aufgabenstellung liegt unter `Sinc/Application-Updater.pdf`. Die mitgelieferten EXE-Dateien liegen unter `Sinc/SampleApplication/<Version>/SampleApplication.exe`.

Alle folgenden PowerShell-Befehle gehen vom Repository-Stamm aus. In Rider kannst du `Sinc.slnx` öffnen.

## Build und Tests

```powershell
dotnet build .\Sinc.slnx
dotnet test .\Sinc.slnx
```

Die Tests prüfen unter anderem die Versionsauswahl, die HTTP-Endpunkte und das Aufräumen nach einem abgebrochenen Download.

## Demo

### 1. Lokale Installation vorbereiten

Schließe eine eventuell laufende SampleApplication, bevor du die Demokopie anlegst oder zurücksetzt.

```powershell
New-Item -ItemType Directory -Path .\demo -Force | Out-Null
Copy-Item -LiteralPath .\Sinc\SampleApplication\1.0.172.5\SampleApplication.exe -Destination .\demo\SampleApplication.exe -Force
(Get-Item -LiteralPath .\demo\SampleApplication.exe).VersionInfo.FileVersion
```

Die Ausgabe zeigt `1.0.172.5`. `demo/` ist die lokale Installation und wird von Git ignoriert. Der Bootloader braucht dort Schreibrechte für den Download und den Dateiaustausch. Mit denselben Befehlen setzt du die Demo für einen weiteren Durchlauf zurück.

### 2. UpdateService starten

```powershell
dotnet run --project .\src\ApplicationUpdater.Service
```

Lass den Dienst für beide Durchläufe geöffnet. In einem zweiten Terminal kannst du die neueste Version abfragen.

```powershell
Invoke-RestMethod -Uri http://localhost:5080/api/updates/latest
```

Der Dienst liefert `2.0.577.98` und den Dateinamen `SampleApplication.exe`.

### 3. Update ausführen und prüfen

```powershell
dotnet run --project .\src\ApplicationUpdater.Bootloader
(Get-Item -LiteralPath .\demo\SampleApplication.exe).VersionInfo.FileVersion
```

Der Bootloader meldet zuerst `1.0.172.5` als installierte Version und anschließend `Update installiert: 2.0.577.98`. Die Dateiversion ist jetzt `2.0.577.98`. Die SampleApplication läuft nach dem Ende des Bootloaders weiter.

### 4. Start ohne Update prüfen

Schließe die SampleApplication und starte den Bootloader erneut.

```powershell
dotnet run --project .\src\ApplicationUpdater.Bootloader
```

Der Bootloader meldet `Kein Update erforderlich.` und startet die SampleApplication erneut. Schließe nach der Demo die Anwendung und beende den UpdateService mit `Strg+C` im Dienstterminal.

## Aufbau und Konfiguration

- **Bootloader** liest die Dateiversion und vergleicht sie mit dem Ergebnis von `GET /api/updates/latest`. Nur bei einer neueren Version lädt er die EXE über `GET /api/updates/{version}/download`. Die URL adressiert die beim Vergleich ausgewählte Version. `ApplicationPath` und `UpdateService:BaseUrl` stehen in `src/ApplicationUpdater.Bootloader/appsettings.json`. Relative Anwendungspfade beziehen sich auf das Verzeichnis des gebauten Bootloaders.
- **UpdateService** implementiert `IUpdateService` und delegiert die Dateizugriffe an `IUpdateStorage`. Der Dienst liest `Urls`, `UpdateStorage:Provider` und `UpdateStorage:RootPath` aus `src/ApplicationUpdater.Service/appsettings.json`. Relative Speicherpfade beziehen sich beim gezeigten Start auf das Verzeichnis des Serviceprojekts. Der unterstützte Anbieter heißt `FileSystem`.
- **Dateisystemspeicher** findet die verfügbaren Versionen in den direkten Unterordnern des konfigurierten Speicherverzeichnisses. Lege für eine neue Serverversion einen Ordner mit der vollständigen vierteiligen Dateiversion an und speichere die passende Datei darin als `SampleApplication.exe`.

Für einen späteren Datenbankspeicher ist der Konfigurationswert `Database` reserviert. Bis eine Datenbankimplementierung von `IUpdateStorage` in `UpdateStorageRegistration` registriert ist, beendet der Dienst bei dieser Auswahl den Start mit einer Fehlermeldung.

Einen anderen lokalen EXE-Pfad kannst du für einen einzelnen Start übergeben.

```powershell
dotnet run --project .\src\ApplicationUpdater.Bootloader -- '--ApplicationPath=C:\MeineInstallation\SampleApplication.exe'
```

## Verhalten bei Fehlern

- Scheitert die Versionsabfrage, der Download oder der Dateiaustausch, meldet der Bootloader den Fehler und startet die vorhandene Installation. Er liefert in diesem Fall Exitcode `1`.
- Bei einem abgebrochenen Download löscht der Bootloader die Teildatei. Scheitert der Austausch nach einem vollständigen Download, bleibt die `.tmp`-Datei erhalten und der Bootloader gibt ihren Pfad aus. Schließe bei einer gesperrten EXE die SampleApplication vor dem nächsten Versuch.
- Fehlt die lokale EXE oder ihre Versionsinformation, beendet sich der Bootloader mit einer Fehlermeldung und Exitcode `1`. Für die Erstinstallation dient Schritt 1 der Demo.
