import { Component, OnInit } from '@angular/core';
import { NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
    selector: 'app-basic-info',
    templateUrl: './basic-info.component.html',
    styleUrls: ['./basic-info.component.css']
})
export class BasicInfoComponent implements OnInit {

    constructor(
        private router: Router,
        private route: ActivatedRoute) {
    }

    onSubmit(signUpForm: NgForm){
        
    }

    ngOnInit(): void {
    }



}
