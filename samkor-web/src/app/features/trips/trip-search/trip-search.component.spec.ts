import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TripSearchComponent } from './trip-search.component';
import { AuthResponse } from '../../../core/models/auth.models';

describe('TripSearchComponent', () => {
  let fixture: ComponentFixture<TripSearchComponent>;
  let httpMock: HttpTestingController;

  const futureDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();

  beforeEach(async () => {
    localStorage.clear();

    // Logger "ind" som driver-1 direkte via localStorage, så
    // AuthService's session-state er sat, før komponenten oprettes
    const authResponse: AuthResponse = {
      userId: 'driver-1',
      token: 'fake-jwt-token',
      expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
      fullName: 'Driver Driversen',
      email: 'driver@test.dk',
      roles: ['User'],
    };
    localStorage.setItem('samkor_token', authResponse.token);
    localStorage.setItem('samkor_user', JSON.stringify(authResponse));

    await TestBed.configureTestingModule({
      imports: [TripSearchComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(TripSearchComponent);
    httpMock = TestBed.inject(HttpTestingController);

    fixture.detectChanges(); // trigger ngOnInit -> search()

    const req = httpMock.expectOne((r) => r.method === 'GET' && r.url.includes('/api/Trips'));
    req.flush([
      {
        id: 'trip-own',
        driverId: 'driver-1',
        driverFullName: 'Driver Driversen',
        fromCity: 'København',
        toCity: 'Aarhus',
        departureTime: futureDate,
        availableSeats: 2,
        pricePerSeat: 100,
        vehicleId: null,
      },
      {
        id: 'trip-other',
        driverId: 'driver-2',
        driverFullName: 'En Anden Chauffør',
        fromCity: 'Odense',
        toCity: 'Vejle',
        departureTime: futureDate,
        availableSeats: 1,
        pricePerSeat: 80,
        vehicleId: null,
      },
    ]);

    fixture.detectChanges();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('viser "Din egen tur" i stedet for en bookingknap på brugerens egen tur', () => {
    const cards = fixture.nativeElement.querySelectorAll('.trip-card');
    const ownCard = Array.from(cards as NodeListOf<HTMLElement>).find((card) =>
      card.textContent?.includes('København'),
    );

    expect(ownCard).toBeTruthy();
    expect(ownCard!.textContent).toContain('Din egen tur');
    expect(ownCard!.querySelector('.actions button')).toBeNull();
  });

  it('viser en "Anmod om plads"-knap på andres ture', () => {
    const cards = fixture.nativeElement.querySelectorAll('.trip-card');
    const otherCard = Array.from(cards as NodeListOf<HTMLElement>).find((card) =>
      card.textContent?.includes('Odense'),
    );

    expect(otherCard).toBeTruthy();
    const button = otherCard!.querySelector('.actions button');
    expect(button?.textContent?.trim()).toContain('Anmod om plads');
  });
});
