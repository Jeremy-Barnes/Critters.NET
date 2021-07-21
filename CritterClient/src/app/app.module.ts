import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { SignupComponent } from './signup/signup.component';
import { BasicInfoComponent } from './signup/basic-info/basic-info.component';
import { FormsModule } from '@angular/forms';
import { UserDetailsComponent } from './signup/user-details/user-details.component';
import { PetCreateComponent } from './signup/pet-create/pet-create.component';
import { ProfileComponent } from './profile/profile.component';
import { SocialComponent } from './social/social.component';
import { MessagePopupComponent } from './message-popup/message-popup.component';

@NgModule({
  declarations: [
    AppComponent,
    DashboardComponent,
    SignupComponent,
    BasicInfoComponent,
    UserDetailsComponent,
    PetCreateComponent,
    ProfileComponent,
    SocialComponent,
    MessagePopupComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
