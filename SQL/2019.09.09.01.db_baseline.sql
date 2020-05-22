CREATE TABLE IF NOT EXISTS users(
    userID SERIAL NOT NULL PRIMARY KEY,
    userName VARCHAR(24) NOT NULL,
    firstName VARCHAR(24),
    lastName VARCHAR(24),
    emailAddress VARCHAR(100) NOT NULL,
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

ALTER TABLE users ADD COLUMN dateJoined TIMESTAMP not null DEFAULT NOW();