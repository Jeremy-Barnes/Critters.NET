import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute, ParamMap } from '@angular/router';

@Component({
    selector: 'app-signup',
    templateUrl: './signup.component.html',
    styleUrls: ['./signup.component.css']
})
export class SignupComponent implements OnInit {

    constructor(
        private router: Router, 
        private route: ActivatedRoute)  {
        
    }

    ngOnInit(): void {
        let link = ['0'];
        this.router.navigate(link, { relativeTo: this.route });
    }

    onSubmit() {
    }

}
