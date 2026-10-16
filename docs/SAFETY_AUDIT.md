# Verifica della sicurezza della cancellazione — 16 ottobre 2026

Per il controllo successivo e i risultati aggiornati vedere la
[seconda ricerca di bug](SECOND_AUDIT.md).

La verifica si concentra sul percorso scansione → database → gruppi di duplicati →
cancellazione e sulla rimozione del database. Tutte le prove usano dati creati in
cartelle temporanee. Il database e le foto dell'utente non sono stati usati.

## Problemi riprodotti e correzioni

| Priorità | Problema precedente | Correzione |
| --- | --- | --- |
| Critica | Lo stesso file registrato con maiuscole diverse o percorsi equivalenti poteva essere sia copia da conservare sia candidato. La cancellazione poteva eliminare l'unica copia. | Rifiuto dei percorsi equivalenti; mantenimento della copia conservata aperta senza condivisione di scrittura/cancellazione. Gli alias fisici non possono essere aperti per la cancellazione mentre la copia è bloccata. |
| Alta | La copia da conservare veniva controllata una volta e poi chiusa. Una modifica/rimozione durante il gruppo rendeva invalida la garanzia di conservare una copia identica. | Il blocco della copia conservata resta attivo per tutto il gruppo, comprese le notifiche di avanzamento. |
| Alta | Il candidato veniva verificato e poi riaperto implicitamente da `File.Delete(path)`. Il file a quel percorso poteva cambiare fra controllo e cancellazione. | Apertura esclusiva del candidato e cancellazione tramite lo stesso handle usato per le verifiche. |
| Alta | Il servizio di basso livello accettava hash assenti e controllava i file contro i rispettivi hash senza richiedere che coincidessero fra loro. Decisioni incoerenti potevano cancellare file diversi. Il normale generatore di gruppi applicava già un filtro sugli hash. | Verifica obbligatoria di hash SHA-256, dimensioni e coerenza del gruppo; confronto integrale dei byte del candidato e della copia conservata prima della cancellazione. La dimensione zero viene verificata esattamente. |
| Critica | La rimozione del database e il reset prima dell'analisi cancellavano qualsiasi file al percorso configurato, compresa una foto. | Controllo della firma SQLite prima di eliminare qualunque file; blocco esclusivo del database e dei suoi file accessori. Se il database è aperto/inaccessibile/non valido, l'operazione viene rifiutata. |

Le prime prove aggiunte hanno prodotto **7 fallimenti su 8 casi**, con la versione
precedente, mentre i 45 test esistenti risultavano tutti superati. Il test completo
scansione → report → cancellazione copre anche l'unica foto acquisita due volte con
maiuscole diverse nel percorso.

Ulteriori protezioni:

- Le decisioni vengono copiate prima dell'esecuzione, così i callback non possono
  cambiare i percorsi approvati mentre la cancellazione procede.
- La rimozione dei record verifica sia l'ID sia il percorso e conta le righe
  effettivamente eliminate. Un errore di accesso non viene trattato come prova
  dell'assenza di un file.
- I percorsi di collegamenti simbolici/file reparse, file speciali e stream
  alternativi vengono rifiutati dal livello di cancellazione. I collegamenti
  fisici alla copia conservata vengono saltati.
- La stringa di connessione SQLite usa il builder, preservando nomi di file
  contenenti punti e virgola.
- La conferma WPF mostra il database, usa «No» come scelta iniziale e dichiara che
  la cancellazione è definitiva e riguarda tutte le cartelle registrate. Il
  percorso confermato viene acquisito prima del lavoro in background.

## Verifica

```powershell
dotnet test FotoCreaDB.sln --no-restore --verbosity minimal
dotnet build FotoCreaDB.sln --no-restore --verbosity minimal
```

Esito finale: **60 test superati** (30 unitari e 30 di integrazione), nessun test
fallito o ignorato; compilazione dell'intera soluzione, WPF inclusa, con **zero
errori**. Restano avvisi preesistenti di configurazione SDK/nullable. La conferma
WPF è stata verificata tramite compilazione e revisione del codice, senza una
prova interattiva della finestra.

La suite comprende casi per: percorsi equivalenti, maiuscole diverse, contenuti
diversi con dimensione uguale, hash mancanti, modifiche dopo l'analisi, tentativi di
modificare/rimuovere la copia conservata durante il gruppo, collegamenti fisici,
percorsi lunghi, candidati in uso, foto configurate come database, database in uso,
reset di un database valido e nomi di database contenenti punti e virgola. Restano
attivi i test precedenti sui gruppi di tre copie, sulla scelta della copia da
conservare e sulla bonifica dei record orfani.

## Comportamento e limiti

- La cancellazione protetta richiede Windows. Se il sistema o il filesystem non
  consentono i blocchi/le operazioni richiesti, i file vengono saltati. Le prove
  locali non certificano ogni NAS, filesystem remoto o servizio cloud.
- Un candidato aperto da altri programmi può essere saltato anche se è un
  duplicato. I file reparse, inclusi alcuni file cloud, vengono rifiutati.
- Il confronto completo richiede letture aggiuntive: questa scelta privilegia la
  sicurezza rispetto alla velocità.
- Il report rimane una lista di candidati basata sul database: la verifica
  effettiva avviene durante la cancellazione. Conteggi del report e cancellazioni
  effettive possono quindi differire.
- La selezione della copia da conservare mantiene la regola esistente: percorso
  più lungo, poi data di modifica più recente, poi ID. Non valuta il valore
  organizzativo di una cartella e non tiene conto di eventuali file sidecar delle
  foto (per esempio XMP).
- I file vengono rimossi definitivamente, senza Cestino o quarantena. Non viene
  introdotto un meccanismo di recupero. Prima di usare questa versione su una
  collezione importante serve una copia di backup indipendente.
- Disco e SQLite non formano una singola transazione: un arresto dopo la rimozione
  fisica può lasciare un record orfano. L'ordine resta disco prima del record e la
  copia conservata non viene cancellata.
- La firma SQLite evita lo scambio accidentale con una foto; non dimostra che un
  database SQLite appartenga a questa applicazione.

Questa revisione e i test riducono rischi concreti e riproducibili; non costituiscono
una prova di assenza di ogni possibile bug o di protezione da guasti hardware e
modifiche che aggirano le normali regole di condivisione del filesystem.

Riferimenti delle API Windows impiegate:
[CreateFileW](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilew),
[SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle),
[FILE_DISPOSITION_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info).
