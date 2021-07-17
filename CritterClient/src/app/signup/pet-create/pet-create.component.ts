import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { User } from 'src/app/dto';
import { PetService } from 'src/app/pet.service';
import { UserService } from 'src/app/user.service';

@Component({
  selector: 'app-pet-create',
  templateUrl: './pet-create.component.html',
  styleUrls: ['./pet-create.component.css']
})
export class PetCreateComponent implements OnInit {


    public userState = new User();
    public passwordConfirm = '';
    public passwordsMatch = false;
    constructor(
        private router: Router,
        private route: ActivatedRoute,
        public petService: PetService,
        public userService: UserService) {
            this.userService.userSubject.subscribe(u => {
                this.userState = u ?? new User();
            });
    }

    onSubmit(signUpForm: NgForm){
    }

    ngOnInit(): void {
    }


}
