import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { User } from 'src/app/dto';
import { UserService } from 'src/app/user.service';

@Component({
    selector: 'app-basic-info',
    templateUrl: './basic-info.component.html',
    styleUrls: ['./basic-info.component.css']
})
export class BasicInfoComponent implements OnInit {

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
    updateGlobalUser(user: User){
        this.userService.activeUserSubject.next(user);
    }

    onSubmit(signUpForm: NgForm){
        const link = ['1'];
        this.router.navigate(link, { relativeTo: this.route.parent});
    }

    confirmPassword(){
        this.passwordsMatch = this.passwordConfirm === this.userState.Password;
    }

    ngOnInit(): void {
    }



}
