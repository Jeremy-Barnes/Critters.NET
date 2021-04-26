import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { PetSpeciesConfig, PetColorConfig } from './dto';
import { environment } from './../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, Observer, throwError } from 'rxjs';
import { catchError, map, retry } from 'rxjs/operators';


@Injectable({
  providedIn: 'root'
})
export class PetService {

    private jwtToken: string;

    constructor(private http: HttpClient) { 
        this.jwtToken = '';
    }

    getColors() : Observable<PetColorConfig[]>{
        let email = null;
        let userName = null;
     
        return this.http.get(environment.apiUrl + "/pet/colors/", 
        {
            withCredentials : true,
        }).pipe(
            retry(2),
            catchError(this.handleError),
            map((r : any) => { return <PetColorConfig[]>r.Colors})
        );
    }

    getSpecies() : Observable<PetSpeciesConfig[]>{
        let email = null;
        let userName = null;
     
        return this.http.get(environment.apiUrl + "/pet/species/", 
        {
            withCredentials : true,
        }).pipe(
            retry(2),
            catchError(this.handleError),
            map((r : any) => { return <PetSpeciesConfig[]>r.Species})
        );
    }

    private handleError(error: HttpErrorResponse) {
        if (error.error instanceof ErrorEvent) {
          // A client-side or network error occurred. Handle it accordingly.
          console.error('An error occurred:', error.error.message);
        } else {
          // The backend returned an unsuccessful response code.
          // The response body may contain clues as to what went wrong.
          console.error(
            `Backend returned code ${error.status}, ` +
            `body was: ${error.error}`);
        }
        // Return an observable with a user-facing error message.
        return throwError(
          'Something bad happened; please try again later.');
    }
}
