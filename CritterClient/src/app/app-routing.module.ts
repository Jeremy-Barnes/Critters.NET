import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { DashboardComponent } from './dashboard/dashboard.component';
import { ProfileComponent } from './profile/profile.component';
import { BasicInfoComponent } from './signup/basic-info/basic-info.component';
import { PetCreateComponent } from './signup/pet-create/pet-create.component';
import { SignupComponent } from './signup/signup.component';
import { UserDetailsComponent } from './signup/user-details/user-details.component';


const routes: Routes = [
    { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
    { path: 'dashboard', component: DashboardComponent },
    { path: 'signup', component: SignupComponent,
        children: [ {path: '0', component: BasicInfoComponent }, {path: '1', component: UserDetailsComponent }, {path: '2', component: PetCreateComponent } ]

    },
    { path: 'user/:userName', component: ProfileComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
