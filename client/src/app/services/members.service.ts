import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';
import { Member } from 'src/app/models/Member';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MembersService {
  baseUrl = environment.apiUrl;
  usersUrl = this.baseUrl + 'users/';
  members: Member[] = [];

  constructor(private http: HttpClient) { }

  getMembers() {
    if (this.members.length > 0)
      return of(this.members);
    else
      return this.http.get<Member[]>(this.usersUrl).pipe(
        map(members => {
          this.members = members;
          return members;
        })
      );
  }

  getMember(username: string) {
    const member = this.members.find(u => u.username === username);
    
    if (member !== undefined)
      return of(member);
    else
      return this.http.get<Member>(this.usersUrl + username);
  }

  updateMember(member: Member) {
    return this.http.put(this.usersUrl, member).pipe(
      map( () => {
        const index = this.members.indexOf(member);
        this.members[index] = member;
      })
    );
  }

  setMainPhoto(photoId: number) {
    return this.http.put(`${this.usersUrl}set-main-photo/${photoId}`, {});
  }

  deletePhoto(photoId: number) {
    return this.http.delete(`${this.usersUrl}delete-photo/${photoId}`);
  }
}
