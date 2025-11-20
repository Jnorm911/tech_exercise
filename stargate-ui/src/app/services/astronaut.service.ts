import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Person {
  name: string;
}

export interface AstronautDuty {
  title: string;
  rank: string;
  startDate: string;
  endDate?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AstronautService {
  private baseUrl = 'http://localhost:5204';

  constructor(private http: HttpClient) {}

  getPersonByName(name: string): Observable<Person> {
    return this.http.get<Person>(`${this.baseUrl}/person/${encodeURIComponent(name)}`);
  }

  getAstronautDutiesByName(name: string): Observable<AstronautDuty[]> {
    return this.http.get<AstronautDuty[]>(`${this.baseUrl}/astronaut-duty/${encodeURIComponent(name)}`);
  }

  getPeople(): Observable<Person[]> {
    return this.http.get<Person[]>(`${this.baseUrl}/person`);
  }

  createPerson(name: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/person`, { name });
  }

  updatePerson(currentName: string, newName: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/person`, { currentName, newName });
  }

  createAstronautDuty(personName: string, duty: AstronautDuty): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/astronaut-duty`, {
      personName,
      dutyTitle: duty.title,
      rank: duty.rank,
      dutyStartDate: duty.startDate,
      dutyEndDate: duty.endDate
    });
  }
}