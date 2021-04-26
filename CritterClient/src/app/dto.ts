export class User {
    constructor(){
        this.UserName = "";
        this.FirstName = "";
        this.LastName = "";
        this.EmailAddress = ""; 
        this.Cash = 0;
        this.Gender = "";
        this.Birthdate = new Date();
        this.City = "";
        this.State = ""
        this.Country = "";
        this.Postcode = "";
        this.Password = "";
    }

    public UserName : string;
    public FirstName : string;
    public LastName : string;
    public EmailAddress : string;
    public Cash : number;
    public Gender : string;
    public Birthdate : Date;
    public City : string;
    public State : string;
    public Country : string;
    public Postcode : string; 
    public Password : string;
}

export class AuthResponse {
    public AuthToken! : string;
    public User!: User;
}

export class PetColorConfig {
    constructor(){
        this.PetColorConfigId = -1;
        this.Name = "";
        this.ImagePatternPath = "";
    }

    public PetColorConfigId : number;
    public Name : string;
    public ImagePatternPath : string;
}

export class PetSpeciesConfig
{
    public PetSpeciesConfigId!: number;
    public Name!: number;
    public MaxHitPoints!: number;
    public Description!: string;
    public ImageBasePath!: string;
}