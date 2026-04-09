using System;
using System.Collections.Generic;
using System.IO;

namespace Foto_CreaDB2
{
    /// <summary>
    /// Gestisce la scansione dei percorsi configurati, l'elaborazione dei file trovati
    /// e l'aggiornamento del repository con i dati raccolti.
    /// </summary>
    public class FileScanner
    {
        private readonly AppConfig _config;
        private readonly FotoRepository _repository;
        private readonly MetadataService _metadataService;
        private readonly HashService _hashService;
        private readonly Logger _logger;
        private readonly ScanStatistics _stats;
        private readonly string _currentScanToken;
        private readonly IProgress<AnalysisProgress> _progress;
        private readonly Action<ServiceLogMessage> _log;

        private int _totalFilesToProcess;
        private int _processedFiles;

        /// <summary>
        /// Inizializza una nuova istanza dello scanner con tutti i servizi necessari alla scansione.
        /// </summary>
        /// <param name="config">
        /// Configurazione applicativa contenente percorsi e opzioni di scansione.
        /// </param>
        /// <param name="repository">
        /// Repository usato per leggere e salvare i dati delle foto nel database.
        /// </param>
        /// <param name="metadataService">
        /// Servizio incaricato di leggere e popolare i metadati del file.
        /// </param>
        /// <param name="hashService">
        /// Servizio incaricato di calcolare l'hash del contenuto del file.
        /// </param>
        /// <param name="logger">
        /// Componente usato per il logging di errori, avanzamento e riepiloghi.
        /// Può essere null se il chiamante gestisce il logging in altro modo.
        /// </param>
        /// <param name="stats">
        /// Oggetto che raccoglie le statistiche cumulative della scansione.
        /// </param>
        /// <param name="progress">
        /// Callback di avanzamento dell'analisi.
        /// </param>
        /// <param name="log">
        /// Callback di log strutturato indipendente dalla console.
        /// </param>
        public FileScanner(
            AppConfig config,
            FotoRepository repository,
            MetadataService metadataService,
            HashService hashService,
            Logger logger,
            ScanStatistics stats,
            IProgress<AnalysisProgress> progress = null,
            Action<ServiceLogMessage> log = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
            _logger = logger;
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _currentScanToken = Guid.NewGuid().ToString("N");
            _progress = progress;
            _log = log;
        }

        /// <summary>
        /// Avvia la scansione di tutti i percorsi configurati.
        /// Per ogni file o cartella valida esegue l'elaborazione e aggiorna le statistiche finali.
        /// </summary>
        public void Scan()
        {
            if (_config.Paths == null || _config.Paths.Length == 0)
            {
                throw new ArgumentException("Non è stato specificato alcun path da analizzare.");
            }

            _stats.TotalePathIniziali = _config.Paths.Length;

            _totalFilesToProcess = CountProcessableFiles();
            _processedFiles = 0;

            ServiceCallbackHelper.Info(_log, "Conteggio file da analizzare completato: " + _totalFilesToProcess);
            ReportProgress(null);

            foreach (string path in _config.Paths)
            {
                try
                {
                    string fullPath = Path.GetFullPath(path);

                    if (File.Exists(fullPath))
                    {
                        _stats.TotaleFileTrovati++;

                        string estensione = Utility.GetEstensione(fullPath);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            _stats.TotaleFileConEstensioneValida++;
                            ProcessFile(fullPath);
                        }
                    }
                    else if (System.IO.Directory.Exists(fullPath))
                    {
                        ProcessDirectory(fullPath);

                        int marcatiMancanti = _repository.MarkMissingFilesForRoot(fullPath, _currentScanToken);
                        _stats.TotaleFileSegnatiComeMancanti += marcatiMancanti;
                    }
                    else
                    {
                        throw new FileNotFoundException($"Path non valido o non trovato: '{fullPath}'.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.WriteError($"Errore durante l'analisi del path iniziale '{path}'", ex);
                    ServiceCallbackHelper.Error(_log, $"Errore durante l'analisi del path iniziale '{path}'", ex);
                }
            }

            ReportProgress(null);

            _logger?.WriteFinalStatistics(_stats);
            ServiceCallbackHelper.Info(_log, "Scansione completata.");
        }

        /// <summary>
        /// Conta il numero totale di file effettivamente processabili
        /// considerando solo i percorsi validi e le estensioni consentite.
        /// </summary>
        /// <returns>
        /// Numero totale di file candidati all'analisi.
        /// </returns>
        private int CountProcessableFiles()
        {
            int total = 0;

            foreach (string path in _config.Paths)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    string fullPath = Path.GetFullPath(path);

                    if (File.Exists(fullPath))
                    {
                        string estensione = Utility.GetEstensione(fullPath);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            total++;
                        }
                    }
                    else if (System.IO.Directory.Exists(fullPath))
                    {
                        total += CountFilesInDirectory(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    ServiceCallbackHelper.Warning(_log, $"Impossibile contare i file nel path '{path}': {ex.Message}");
                }
            }

            return total;
        }

        /// <summary>
        /// Conta ricorsivamente i file processabili presenti in una cartella.
        /// </summary>
        /// <param name="targetDirectory">
        /// Cartella da analizzare.
        /// </param>
        /// <returns>
        /// Numero di file con estensione valida.
        /// </returns>
        private int CountFilesInDirectory(string targetDirectory)
        {
            int total = 0;

            try
            {
                foreach (string fileName in System.IO.Directory.EnumerateFiles(targetDirectory))
                {
                    try
                    {
                        string estensione = Utility.GetEstensione(fileName);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            total++;
                        }
                    }
                    catch
                    {
                    }
                }

                foreach (string subdirectory in System.IO.Directory.EnumerateDirectories(targetDirectory))
                {
                    total += CountFilesInDirectory(subdirectory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                ServiceCallbackHelper.Warning(_log, $"Accesso negato durante il conteggio della cartella '{targetDirectory}': {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                ServiceCallbackHelper.Warning(_log, $"Percorso troppo lungo durante il conteggio della cartella '{targetDirectory}': {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                ServiceCallbackHelper.Warning(_log, $"Cartella non trovata durante il conteggio '{targetDirectory}': {ex.Message}");
            }
            catch (Exception ex)
            {
                ServiceCallbackHelper.Warning(_log, $"Errore generico durante il conteggio della cartella '{targetDirectory}': {ex.Message}");
            }

            return total;
        }

        /// <summary>
        /// Analizza ricorsivamente una cartella, elaborando i file compatibili
        /// e scendendo nelle eventuali sottocartelle.
        /// </summary>
        /// <param name="targetDirectory">
        /// Cartella da scandire.
        /// </param>
        private void ProcessDirectory(string targetDirectory)
        {
            try
            {
                _stats.TotaleCartelleVisitate++;

                foreach (string fileName in System.IO.Directory.EnumerateFiles(targetDirectory))
                {
                    try
                    {
                        _stats.TotaleFileTrovati++;

                        string estensione = Utility.GetEstensione(fileName);
                        if (_config.EstensioniPermesse.Contains(estensione))
                        {
                            _stats.TotaleFileConEstensioneValida++;
                            ProcessFile(fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _stats.TotaleErroriFile++;
                        _logger?.WriteError($"Errore durante la lavorazione del file '{fileName}'", ex);
                        ServiceCallbackHelper.Error(_log, $"Errore durante la lavorazione del file '{fileName}'", ex);
                    }
                }

                foreach (string subdirectory in System.IO.Directory.EnumerateDirectories(targetDirectory))
                {
                    ProcessDirectory(subdirectory);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger?.WriteError($"Accesso negato alla cartella '{targetDirectory}'", ex);
                ServiceCallbackHelper.Error(_log, $"Accesso negato alla cartella '{targetDirectory}'", ex);
            }
            catch (PathTooLongException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger?.WriteError($"Percorso troppo lungo '{targetDirectory}'", ex);
                ServiceCallbackHelper.Error(_log, $"Percorso troppo lungo '{targetDirectory}'", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger?.WriteError($"Cartella non trovata '{targetDirectory}'", ex);
                ServiceCallbackHelper.Error(_log, $"Cartella non trovata '{targetDirectory}'", ex);
            }
            catch (Exception ex)
            {
                _stats.TotaleErroriCartelle++;
                _logger?.WriteError($"Errore generico nella cartella '{targetDirectory}'", ex);
                ServiceCallbackHelper.Error(_log, $"Errore generico nella cartella '{targetDirectory}'", ex);
            }
        }

        /// <summary>
        /// Elabora un singolo file: controlla se può essere saltato,
        /// legge i metadati, calcola l'hash, genera le chiavi derivate
        /// e salva il risultato nel database.
        /// </summary>
        /// <param name="imagePath">
        /// Percorso completo del file da elaborare.
        /// </param>
        private void ProcessFile(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("Il path del file è vuoto.", nameof(imagePath));
            }

            try
            {
                _stats.TotaleFileElaborati++;

                FileInfo fInfo = new FileInfo(imagePath);

                if (!fInfo.Exists)
                {
                    throw new FileNotFoundException($"File non trovato: '{imagePath}'.");
                }

                string dataFileModifica = fInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                long dimensione = fInfo.Length;

                ExistingFotoInfo? existing = _repository.GetByPercorso(imagePath);

                if (existing != null
                    && string.Equals(existing.DataFileModifica, dataFileModifica, StringComparison.Ordinal)
                    && existing.Dimensione == dimensione)
                {
                    _repository.TouchAsSeenWithoutRehash(imagePath, _currentScanToken);
                    _stats.TotaleFileSaltati++;

                    Foto fotoSaltata = BuildBaseFoto(fInfo, dataFileModifica, dimensione);
                    fotoSaltata.dataScansione = _currentScanToken;
                    fotoSaltata.fileEsiste = true;

                    _logger?.WriteFileProcessed("SKIP", fotoSaltata, _stats);
                    ServiceCallbackHelper.Info(_log, "SKIP: " + imagePath);
                    return;
                }

                Foto foto = BuildBaseFoto(fInfo, dataFileModifica, dimensione);
                foto.dataScansione = _currentScanToken;
                foto.fileEsiste = true;

                bool metadatiLetti = _metadataService.TryPopulateMetadata(imagePath, foto, _logger, _stats);
                if (!metadatiLetti)
                {
                    _stats.TotaleFileSenzaMetadatiUtili++;
                }

                bool hashCalcolato = _hashService.TryCalculateHash(imagePath, foto, _logger, _stats);
                if (!hashCalcolato)
                {
                    foto.noteErrore = "Hash non calcolato.";
                }

                foto.CalcolaChiaviDerivate();

                try
                {
                    if (existing == null)
                    {
                        _repository.Insert(foto);
                        _stats.TotaleFileInseriti++;
                        _logger?.WriteFileProcessed("INSERT", foto, _stats);
                        ServiceCallbackHelper.Info(_log, "INSERT: " + imagePath);
                    }
                    else
                    {
                        _repository.Update(foto);
                        _stats.TotaleFileAggiornati++;
                        _logger?.WriteFileProcessed("UPDATE", foto, _stats);
                        ServiceCallbackHelper.Info(_log, "UPDATE: " + imagePath);
                    }
                }
                catch (Exception ex)
                {
                    _stats.TotaleErroriDb++;
                    throw new Exception($"Errore DB sul file '{imagePath}'.", ex);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _stats.TotaleErroriFile++;
                _logger?.WriteError($"Accesso negato al file '{imagePath}'", ex);
                ServiceCallbackHelper.Error(_log, $"Accesso negato al file '{imagePath}'", ex);
            }
            catch (PathTooLongException ex)
            {
                _stats.TotaleErroriFile++;
                _logger?.WriteError($"Percorso troppo lungo per il file '{imagePath}'", ex);
                ServiceCallbackHelper.Error(_log, $"Percorso troppo lungo per il file '{imagePath}'", ex);
            }
            catch (IOException ex)
            {
                _stats.TotaleErroriFile++;
                _logger?.WriteError($"Errore I/O sul file '{imagePath}'", ex);
                ServiceCallbackHelper.Error(_log, $"Errore I/O sul file '{imagePath}'", ex);
            }
            catch (Exception ex)
            {
                _stats.TotaleErroriFile++;
                _logger?.WriteError($"Errore non gestito sul file '{imagePath}'", ex);
                ServiceCallbackHelper.Error(_log, $"Errore non gestito sul file '{imagePath}'", ex);
            }
            finally
            {
                _processedFiles++;
                ReportProgress(imagePath);
            }
        }

        /// <summary>
        /// Notifica lo stato di avanzamento corrente dell'analisi.
        /// </summary>
        /// <param name="currentFile">
        /// File attualmente elaborato.
        /// </param>
        private void ReportProgress(string currentFile)
        {
            ServiceCallbackHelper.ReportProgress(
                _progress,
                new AnalysisProgress
                {
                    ProcessedFiles = _processedFiles,
                    TotalFiles = _totalFilesToProcess,
                    CurrentFile = currentFile
                });
        }

        /// <summary>
        /// Costruisce un oggetto <see cref="Foto"/> con i dati base del file,
        /// inizializzando i campi non ancora popolati con valori di default.
        /// </summary>
        /// <param name="fInfo">
        /// Informazioni di file lette dal filesystem.
        /// </param>
        /// <param name="dataFileModifica">
        /// Data di modifica del file già formattata.
        /// </param>
        /// <param name="dimensione">
        /// Dimensione del file in byte.
        /// </param>
        /// <returns>
        /// Un'istanza di <see cref="Foto"/> pronta per essere arricchita con metadati e hash.
        /// </returns>
        private Foto BuildBaseFoto(FileInfo fInfo, string dataFileModifica, long dimensione)
        {
            Foto foto = new Foto
            {
                percorsoCompleto = fInfo.FullName,
                cartella = fInfo.DirectoryName ?? "",
                nomefile = fInfo.Name,
                nomefileNorm = Utility.NormalizeFileNameWithoutExtension(fInfo.Name),
                estensione = Utility.GetEstensione(fInfo.FullName),
                dimensione = dimensione,
                dataFileModifica = dataFileModifica,
                dataScatto = "",
                larghezza = 0,
                altezza = 0,
                marca = "",
                modello = "",
                esposizione = "",
                apertura = "",
                iso = "",
                compensazione = "",
                zoom = "",
                mimeType = "",
                hashSha256 = "",
                chiaveDupBinario = "",
                chiaveDupScatto = "",
                chiaveDupProbabile = "",
                nuovofile = "",
                metadatiPresenti = false,
                noteErrore = ""
            };

            return foto;
        }
    }
}