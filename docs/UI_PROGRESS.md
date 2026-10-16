# Aggiornamenti dell'avanzamento WPF

Analisi, report e cancellazione conservano un solo aggiornamento pendente per
operazione. `Report` sostituisce quel valore sotto un lock breve, senza creare
task o inserire un evento per file nella coda del dispatcher.

Un unico `DispatcherTimer`, con intervallo di 250 ms e priorità Background,
pubblica l'ultimo valore disponibile sul thread grafico. La frequenza effettiva
dipende dalla disponibilità del dispatcher: il timer non è una garanzia di tempo
reale. I valori intermedi vengono accorpati, non accumulati.

I metodi `CompleteAnalysis`, `CompleteReport` e `CompleteDeletion`, chiamati nei
blocchi `finally` del view model, pubblicano subito l'ultimo valore e chiudono
l'operazione, anche dopo un errore o con totale zero. Gli eventi tardivi del
vecchio reporter vengono ignorati. Il reset crea un reporter nuovo, così gli
eventi di una precedente operazione non possono sovrascrivere quella successiva.
Il timer viene fermato quando non ci sono operazioni attive e rilasciato alla
chiusura della finestra.

## Verifica

`FotoCreaDB.Tests.Wpf` esegue cinque test su dispatcher WPF reali, senza aprire
finestre. Una raffica di 150.000 notifiche da un thread separato verifica che non
si creino altrettante operazioni grafiche e che venga pubblicato il conteggio
finale. Gli altri casi coprono completamento parziale, reset, tutte e tre le
operazioni, totale zero e rilascio delle risorse.

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --no-incremental --warnaserror --verbosity minimal
```

Esito: 78 test superati complessivamente; compilazione con zero avvisi ed errori.
Questa è una prova del flusso di notifiche, non un benchmark di lettura di 150.000
foto. La gestione del log per file è un intervento separato e rimane un possibile
collo di bottiglia; scansione, scritture SQLite e verifiche prima della
cancellazione mantengono il comportamento precedente.
