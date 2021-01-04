CREATE COLLATION IF NOT EXISTS case_insensitive_collat (
  provider = 'icu',
  locale = '@colStrength=secondary',
  deterministic = false
);
drop table if exists userMetaphones;
drop index if exists ix_leaderboard_ranking;
drop table if exists leaderboardEntries;
drop table if exists gameConfigs;
drop table if exists friendships;
drop table if exists pets;
drop table if exists petSpeciesConfigs;
drop table if exists petColorConfigs;
drop table if exists readReceipts;
drop table if exists messages;
drop table if exists channelUsers;
drop table if exists channels;
drop table if exists users;


CREATE TABLE IF NOT EXISTS users(
    userID SERIAL NOT NULL PRIMARY KEY,
    userName VARCHAR(24) NOT NULL COLLATE public.case_insensitive_collat,
    firstName VARCHAR(24),
    lastName VARCHAR(24),
    emailAddress VARCHAR(100) NOT NULL COLLATE public.case_insensitive_collat,
    password VARCHAR NOT NULL,
    gender VARCHAR(8) NOT NULL,
    birthdate DATE NOT NULL,
    salt VARCHAR NOT NULL,
    city VARCHAR(50),
    state VARCHAR(50),
    country VARCHAR(50),
    postcode VARCHAR(20),
    cash INT NOT NULL,
    isActive BOOLEAN NOT NULL DEFAULT true,
    isDev BOOLEAN NOT NULL DEFAULT false,
	dateJoined TIMESTAMP not null DEFAULT (NOW() AT TIME ZONE 'utc'),
    CONSTRAINT uk_username UNIQUE (userName),
    CONSTRAINT uk_email UNIQUE (emailAddress),
    CHECK (gender IN ('male','female','other'))
);

CREATE TABLE IF NOT EXISTS channels(
    channelID SERIAL NOT NULL PRIMARY KEY,
    name VARCHAR(50),
    createDate TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

CREATE TABLE IF NOT EXISTS channelUsers(
    channelID INT NOT NULL REFERENCES channels(channelID),
    memberID INT NOT NULL REFERENCES users(userID), 
	PRIMARY KEY (channelID, memberID),
    admin BOOLEAN NOT NULL DEFAULT false,
    joinDate TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

CREATE TABLE IF NOT EXISTS messages(
    messageID SERIAL NOT NULL PRIMARY KEY,
    senderUserID INT NOT NULL REFERENCES users(userID),
    dateSent TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
    messageText TEXT,
    subject VARCHAR(140),
    deleted BOOLEAN NOT NULL DEFAULT false,
    parentMessageID INT null REFERENCES messages(messageID), 
    channelID INT NOT NULL REFERENCES channels(channelID)
);

CREATE TABLE IF NOT EXISTS readReceipts(
    messageID INT NOT NULL REFERENCES messages(messageID),
    recipientID INT NOT NULL REFERENCES users(userID),
    read BOOLEAN NOT NULL DEFAULT false,
    PRIMARY KEY (messageID, recipientID)
);

CREATE TABLE IF NOT EXISTS petSpeciesConfigs(
    petSpeciesConfigID SERIAL NOT NULL PRIMARY KEY,
    name VARCHAR(24) NOT NULL,
    maxHitPoints int NOT NULL,
    description VARCHAR(2000) NOT NULL,
    imageBasePath  VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS petColorConfigs(
    petColorConfigID SERIAL NOT NULL PRIMARY KEY,
    name VARCHAR(24) NOT NULL,
    imagePatternPath varchar(200)
);

CREATE TABLE IF NOT EXISTS pets(
    petID SERIAL NOT NULL PRIMARY KEY,
    name VARCHAR(24) NOT NULL,
    level int NOT NULL default 0,
    currentHitPoints int NOT NULL, 
    gender VARCHAR(8) NOT NULL,
    colorID INT NOT NULL REFERENCES petColorConfigs(petColorConfigID),
    ownerID INT NULL REFERENCES users(userID),
    speciesID INT NOT NULL REFERENCES petSpeciesConfigs(petSpeciesConfigID),
    isAbandoned boolean NOT NULL default false,
    CHECK (gender IN ('male','female','other'))
);

CREATE TABLE IF NOT EXISTS gameConfigs(
    gameConfigID SERIAL NOT NULL PRIMARY KEY,
    isActive BOOLEAN DEFAULT false,
    name VARCHAR(48) NOT NULL,
    description VARCHAR(1000) NOT NULL,
    iconPath VARCHAR(100) NOT NULL,
	cashCap INT NULL,
	dailyCashCountCap INT NULL,
	scoreToCashFactor FLOAT NULL,
	leaderboardMaxSpot INT NULL,
    gameURL VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS leaderboardEntries(
    gameID INT NOT NULL REFERENCES gameConfigs(gameConfigID),
	score INT NOT NULL,
    playerID INT NOT NULL REFERENCES users(userID),
    dateSubmitted TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
	PRIMARY KEY (gameID, playerID)
); 

CREATE INDEX IF NOT EXISTS ix_leaderboard_ranking ON leaderboardEntries(gameID DESC, dateSubmitted DESC, score DESC) WITH (fillfactor = 50);

CREATE TABLE IF NOT EXISTS friendships(
    requesterUserID INT NOT NULL REFERENCES users(userID),
    requestedUserID INT NOT NULL REFERENCES users(userID),
    accepted BOOLEAN NOT NULL DEFAULT false,
    dateSent TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
	PRIMARY KEY (requesterUserID, requestedUserID)
);

CREATE TABLE IF NOT EXISTS userMetaphones(
    metaphone INT NOT NULL,
    soundsLikeUserId INT NOT NULL REFERENCES users(userID),
	PRIMARY KEY (metaphone, soundsLikeUserId)
);

INSERT INTO users(username, firstname, lastname, emailaddress, password, gender, birthdate, salt, city, state, country, postcode, cash, isactive, datejoined, isDev) VALUES 
('TheOneTrueAdmin', 'Nic', 'Cage', 'jabarnes2112@gmail.com', '$2a$11$6wlm9qA4W4DsGZVuncdDouxwrqLrAYkwK2YLZuk6yJKfelGAOtlbi', 'male', '1991-12-05', 
'$2a$11$6wlm9qA4W4DsGZVuncdDou', 'Chicago', 'IL', 'USA', '60613', 1000000000, true, '2020-06-02 20:53:18.636841', true)
