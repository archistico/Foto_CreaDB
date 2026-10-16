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

## Rilevamento iniziale

Appena avviata l'analisi, la scheda mostra «Ricerca dei file…» e una barra
indeterminata. Durante il conteggio preliminare vengono pubblicati il numero
cumulativo di file processabili individuati e la cartella corrente. Il percorso
viene comunicato prima di accedere a una cartella, così resta visibile anche
quando il suo accesso è lento. Gli aggiornamenti dei contatori vengono limitati
anche alla sorgente, evitando un nuovo oggetto per ciascun file contato.

Terminato il rilevamento, la barra passa al totale noto e alla percentuale di
analisi. Una cartella vuota mostra «Nessun file da analizzare». Il completamento,
anche con errore durante il rilevamento, ferma l'animazione. Il conteggio resta
una passata preliminare: questa modifica ne rende visibile l'attività.

## Verifica

`FotoCreaDB.Tests.Wpf` esegue sette test su dispatcher WPF reali, senza aprire
finestre. Una raffica di 150.000 notifiche da un thread separato verifica che non
si creino altrettante operazioni grafiche e che venga pubblicato il conteggio
finale. Gli altri casi coprono completamento parziale, reset, tutte e tre le
operazioni, totale zero, rilascio delle risorse e passaggio dalla ricerca
all'analisi. I test di integrazione verificano il rilevamento prima dell'analisi,
con più cartelle, sottocartelle, file esclusi, file singoli e cartelle vuote.

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --no-incremental --warnaserror --verbosity minimal
```

Esito aggiornato: 101 test superati complessivamente; compilazione con zero avvisi ed errori.
Questa è una prova del flusso di notifiche, non un benchmark di lettura di 150.000
foto. La gestione del log per file è un intervento separato e rimane un possibile
collo di bottiglia; scansione, scritture SQLite e verifiche prima della
cancellazione mantengono il comportamento precedente.

## Elenco duplicati nel log

La casella «Mostra elenco duplicati nel log» si applica alla successiva generazione
del report o al riepilogo prima della cancellazione. Disattivata, produce un
riepilogo compatto; attivata, aggiunge hash del gruppo e percorsi dei file da
conservare/candidati all'eliminazione. Durante un'operazione la casella è disabilitata.

Conteggi, spazio occupato dai duplicati e decisioni vengono calcolati sempre.
L'opzione non nasconde avvisi di sicurezza, errori o registrazioni dei file
effettivamente eliminati e non controlla i log per file della scansione. Il log
già generato rimane consultabile; la casella non è un filtro retroattivo.

Tre test di integrazione verificano l'uguaglianza dei risultati con e senza
dettagli e la conservazione degli avvisi/file modificati dopo l'analisi. Nella
console `--verbose` mantiene il formatter dedicato, senza ripetere l'elenco nel
log del servizio.

La successiva [verifica del report](REPORT_AUDIT.md) documenta le correzioni
all'apertura del database, ai risultati obsoleti, al completamento e alle
prestazioni del raggruppamento.
