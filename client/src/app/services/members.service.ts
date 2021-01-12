import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Member } from 'src/app/models/Member';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  usersUrl = this.baseUrl + 'users/';

  constructor(private https: HttpClient) { }

  getMembers() {
    return this.https.get<Member[]>(this.usersUrl);
  }

  getMember(username: string) {
    return this.https.get<Member>(this.usersUrl + username);
  }
}
