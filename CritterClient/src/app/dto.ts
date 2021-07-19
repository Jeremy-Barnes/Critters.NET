
export class User {
    public UserName: string;
    public FirstName: string;
    public LastName: string;
    public EmailAddress: string;
    public Cash: number;
    public Gender: string;
    public Birthdate: Date;
    public City: string;
    public State: string;
    public Country: string;
    public Postcode: string;
    public Password: string;
}

export class AuthResponse {
    public AuthToken: string;
    public User: User;
}

export class PetColorConfig
{
    public PetColorConfigId: number;
    public Name: string;
    public ImagePatternPath: string;
}

export class PetSpeciesConfig
{
    public PetSpeciesConfigId: number;
    public Name: number;
    public MaxHitPoints: number;
    public Description: string;
    public ImageBasePath: string;
}

export class Pet
{
    public Name!: string;
    public Level!: number;
    public CurrentHitPoints!: number;
    public Gender!: string;
    public SpeciesId!: number;
    public ColorId!: number;
    public IsAbandoned!: boolean;
}

export class SearchResult
{
    public Pets!: Pet[];
    public Users!: User[];
}

export class FriendshipDetails
{
    public RequesterUserName: string;
    public RequestedUserName: string;
    public Friendship: Friendship;
}

export class Friendship
{
    public Accepted: boolean;
    public DateSent: Date;
}

export class MessageResponse {
    public ChannelDetails: ChannelDetails[];
}

export class ChannelDetails {
    public Messages: MessageDetails[];
    public Users: User[];
    public UserNames: string[];
    public Channel: Channel;
}

export class Channel {
    public ChannelId: number;
    public Name: string;
    public CreateDate: string;
}

export class MessageDetails {
    public SenderUserName: string;
    public Message: Message;
}

export class Message { 
    public MessageId: number;
    public DateSent: Date;
    public MessageText: string;
    public Subject: string;
    public ParentMessageId: number;
    public ChannelId: number;
}