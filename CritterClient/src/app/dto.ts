export class User {
    constructor(){
        this.UserName = "";
        this.FirstName = "";
        this.LastName = "";
        this.EmailAddress = ""; 
        this.Cash = 0;
        this.Gender = "";
        this.Birthdate = new Date();
        this.City = "Critterton";
        this.State = ""
        this.Country = "";
        this.Postcode = "60605";
        this.Password = "Password";
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