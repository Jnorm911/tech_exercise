import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AstronautService, Person } from './services/astronaut.service';
import { MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, CommonModule, MatTableModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  // results
  people: Person[] = [];

  // flags
  loading = false;
  saving = false;
  error: string | null = null;
  success: string | null = null;

  // people management
  newPersonName = '';
  updateCurrentName = '';
  updateNewName = '';

  constructor(private astronautService: AstronautService) {}

  ngOnInit(): void {
    this.loadPeople();
  }

  private clearMessages(): void {
    this.error = null;
    this.success = null;
  }

  loadPeople(): void {
    this.clearMessages();
    this.loading = true;

    this.astronautService.getPeople().subscribe({
      next: (result: any) => {
        this.people = result?.people ?? [];
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to load people.';
        this.loading = false;
      }
    });
  }

  createPerson(): void {
    const name = this.newPersonName.trim();
    this.clearMessages();

    if (!name) {
      this.error = 'Enter a name to create.';
      return;
    }

    this.saving = true;

    this.astronautService.createPerson(name).subscribe({
      next: () => {
        this.success = 'Person created.';
        window.alert(`Person added: ${name}`);
        this.newPersonName = '';
        this.saving = false;
        this.loadPeople();
      },
      error: () => {
        this.error = 'Unable to create person.';
        this.saving = false;
      }
    });
  }

  updatePerson(): void {
    const currentName = this.updateCurrentName.trim();
    const newName = this.updateNewName.trim();
    this.clearMessages();

    if (!currentName || !newName) {
      this.error = 'Enter both the current and new name.';
      return;
    }

    this.saving = true;

    this.astronautService.updatePerson(currentName, newName).subscribe({
      next: () => {
        this.success = 'Person updated.';
        window.alert(`Person updated: ${newName}`);
        this.updateCurrentName = '';
        this.updateNewName = '';
        this.saving = false;
        this.loadPeople();
      },
      error: () => {
        this.error = 'Unable to update person.';
        this.saving = false;
      }
    });
  }
}