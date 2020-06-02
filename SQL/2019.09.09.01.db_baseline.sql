CREATE COLLATION case_insensitive_collat (
  provider = 'icu',
  locale = '@colStrength=secondary',
  deterministic = false
);

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
    isActive boolean not null DEFAULT 't',
	dateJoined TIMESTAMP not null DEFAULT NOW(),
    CONSTRAINT uk_username UNIQUE (userName),
    CONSTRAINT uk_email UNIQUE (emailAddress),
    CHECK (gender IN ('male','female','other'))
);

CREATE TABLE IF NOT EXISTS channels(
    channelID SERIAL NOT NULL PRIMARY KEY,
    channelName VARCHAR(50),
    createDate TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS channelUsers(
    channelID INT NOT NULL REFERENCES channels(channelID),
    memberID INT NOT NULL REFERENCES users(userID), 
	PRIMARY KEY (channelID, memberID),
    admin BOOLEAN NOT NULL DEFAULT 'f',
    joinDate TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS messages(
    messageID SERIAL NOT NULL PRIMARY KEY,
    senderUserID INT NOT NULL REFERENCES users(userID),
    dateSent TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    messageText TEXT,
    messageSubject VARCHAR(140),
    deleted BOOLEAN NOT NULL DEFAULT false,
    parentMessageID INT null REFERENCES messages(messageID), 
    channelID INT NOT NULL REFERENCES channels(channelID)
);

CREATE TABLE IF NOT EXISTS readReceipts(
    messageID INT NOT NULL REFERENCES messages(messageID),
    recipientID INT NOT NULL REFERENCES users(userID),
    read BOOLEAN NOT NULL DEFAULT 'f',
    PRIMARY KEY (messageID, recipientID)
);

CREATE TABLE IF NOT EXISTS readReceipts(
    messageID INT NOT NULL REFERENCES messages(messageID),
    recipientID INT NOT NULL REFERENCES users(userID),
    read BOOLEAN NOT NULL DEFAULT 'f',
    PRIMARY KEY (messageID, recipientID)
);

CREATE TABLE IF NOT EXISTS petSpeciesConfigs(
    petSpeciesConfigID SERIAL NOT NULL PRIMARY KEY,
    speciesName VARCHAR(24) NOT NULL,
    maxHitPoints int NOT NULL,
    description VARCHAR(2000) NOT NULL,
    imageBasePath  VARCHAR(200) NOT NULL
);

CREATE TABLE IF NOT EXISTS petColorConfigs(
    petColorConfigID SERIAL NOT NULL PRIMARY KEY,
    colorName VARCHAR(24) NOT NULL,
    imagePatternPath varchar(200)
);

CREATE TABLE IF NOT EXISTS pets(
    petID SERIAL NOT NULL PRIMARY KEY,
    petName VARCHAR(24) NOT NULL,
    level int NOT NULL default 0,
    currentHitPoints int NOT NULL, 
    gender VARCHAR(8) NOT NULL,
    colorID INT NOT NULL REFERENCES petColorConfigs(petColorConfigID),
    ownerID INT NULL REFERENCES users(userID),
    speciesID INT NOT NULL REFERENCES petSpeciesConfigs(petSpeciesConfigID),
    isAbandoned boolean not null default 'f',
    CHECK (gender IN ('male','female','other'))
);
