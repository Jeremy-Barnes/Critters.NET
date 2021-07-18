import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { User } from 'src/app/dto';
import { UserService } from 'src/app/user.service';

@Component({
  selector: 'app-user-details',
  templateUrl: './user-details.component.html',
  styleUrls: ['./user-details.component.css']
})
export class UserDetailsComponent implements OnInit {

    public userState = new User();
    public passwordConfirm = '';
    public passwordsMatch = false;
    constructor(
        private router: Router,
        private route: ActivatedRoute,
        public userService: UserService) {
            this.userService.activeUserSubject.subscribe(u => {
                this.userState = u ?? new User();
            });
    }

    onSubmit(signUpForm: NgForm){
        const link = ['2'];
        this.router.navigate(link, { relativeTo: this.route.parent});
    }

    confirmPassword(){
        this.passwordsMatch = this.passwordConfirm === this.userState.Password;
    }

    ngOnInit(): void {
    }

}
