CREATE DATABASE Ecommerce_BuildWeek;

USE Ecommerce_BuildWeek;

CREATE TABLE Utenti (
    IdUtente UNIQUEIDENTIFIER PRIMARY KEY,
    Nome NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    UserPassword NVARCHAR(255) NOT NULL,
	IsAdmin BIT NOT NULL DEFAULT 0,
);

CREATE TABLE Categorie (
    IdCategoria UNIQUEIDENTIFIER PRIMARY KEY,
    NomeCategoria NVARCHAR(255) NOT NULL
);

CREATE TABLE Prodotti (
    IdProdotto UNIQUEIDENTIFIER PRIMARY KEY,
    URLImmagine NVARCHAR(255) NOT NULL,
    Prezzo DECIMAL(9,2) NOT NULL,
    Dettaglio NVARCHAR(255) NOT NULL,
    Descrizione NVARCHAR(1000) NOT NULL,
    IdCategoria UNIQUEIDENTIFIER,
    Stock INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_IdCategoria FOREIGN KEY (IdCategoria) REFERENCES Categorie(IdCategoria)
);

CREATE TABLE Carrello (
    IdCarrello UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdUtente UNIQUEIDENTIFIER NOT NULL,
    IdProdotto UNIQUEIDENTIFIER NOT NULL,
    Quantita INT NOT NULL CHECK (Quantita > 0),
    CONSTRAINT FK_Carrello_Utente FOREIGN KEY (IdUtente) REFERENCES Utenti(IdUtente),
    CONSTRAINT FK_Carrello_Prodotto FOREIGN KEY (IdProdotto) REFERENCES Prodotti(IdProdotto),
);

-- Creazione delle categorie
INSERT INTO Categorie (IdCategoria, NomeCategoria) VALUES
(NEWID(), 'Yu-Gi-Oh'),
(NEWID(), 'Pokemon'),
(NEWID(), 'DragonBall'),
(NEWID(), 'One Piece'),
(NEWID(), 'Lorcana'),
(NEWID(), 'Magic The Gathering');

-- Recupero degli ID generati per le categorie
DECLARE @YugiOh UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'Yu-Gi-Oh');
DECLARE @Pokemon UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'Pokemon');
DECLARE @Dragonball UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'DragonBall');
DECLARE @OnePiece UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'One Piece');
DECLARE @Lorcana UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'Lorcana');
DECLARE @Magic UNIQUEIDENTIFIER = (SELECT IdCategoria FROM Categorie WHERE NomeCategoria = 'Magic The Gathering');

-- Inserimento dei prodotti (carte collezionabili)
INSERT INTO Prodotti (IdProdotto, URLImmagine, Prezzo, Dettaglio, Descrizione, IdCategoria, Stock) VALUES
(NEWID(), 'https://placecats.com/millie/150/200', 9.99, 'Carta rara di un drago leggendario', 'Un potente drago con attacco devastante.', @YugiOh, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 7.50, 'Guerriero elfico', 'Un elfo con incredibili capacità di combattimento.', @YugiOh, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 12.99, 'Astronave da battaglia', 'Un’astronave futuristica con armi avanzate.', @Pokemon, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 8.00, 'Alieno misterioso', 'Creatura aliena con poteri telepatici.', @Pokemon, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 11.49, 'Zeus, Re degli Dei', 'Una carta mitologica con incredibile potere.', @Dragonball, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 10.99, 'Medusa', 'Trasforma i nemici in pietra con uno sguardo.', @Dragonball, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 13.99, 'Supereroe mascherato', 'Dotato di superforza e volo.', @OnePiece, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 14.50, 'Villain malvagio', 'Un cattivo con un piano di dominio mondiale.', @OnePiece, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 9.00, 'Vampiro immortale', 'Un potente vampiro che si nutre di sangue.', @Lorcana, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 8.99, 'Lupo Mannaro', 'Creatura feroce che si trasforma nelle notti di luna piena.', @Lorcana, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 10.50, 'Samurai leggendario', 'Un maestro della spada del Giappone feudale.', @Magic, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 11.25, 'Cavaliere templare', 'Un guerriero sacro in armatura pesante.', @Magic, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 15.00, 'Strega oscura', 'Un incantatrice con poteri proibiti.', @YugiOh, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 7.80, 'Robot assassino', 'Un droide programmato per la distruzione.', @Pokemon, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 10.99, 'Minotauro', 'Una bestia mitologica metà uomo e metà toro.', @Dragonball, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 13.25, 'Cyborg potenziato', 'Metà uomo, metà macchina, con forza sovrumana.', @OnePiece, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 12.50, 'Mummia maledetta', 'Una creatura avvolta in bende che porta sventure.', @Lorcana, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 9.99, 'Spartano valoroso', 'Un guerriero dell’antica Grecia, imbattibile in battaglia.', @Magic, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 14.75, 'Stregone supremo', 'Un mago con poteri arcani illimitati.', @YugiOh, 3),
(NEWID(), 'https://placecats.com/millie/150/200', 8.50, 'Cacciatore di demoni', 'Un guerriero addestrato a combattere le forze oscure.', @Pokemon, 3);

SELECT * FROM Utenti

INSERT INTO Utenti (IdUtente, Nome, Email, UserPassword, IsAdmin) VALUES 
(NEWID(), 'Admin', 'admin@email.com', 'Admin123', 1),
(NEWID(), 'Admin2', 'admin2@email.com', 'Admin123', 1),
(NEWID(), 'Admin3', 'admin3@email.com', 'Admin123', 1),
(NEWID(), 'PeppePomo', 'BoiaLeCarte@email.com', 'YuGi123', 0),
(NEWID(), 'Vik00', 'Vittorio@email.com', 'PikaPikaChu', 0);


