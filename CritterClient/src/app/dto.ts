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
