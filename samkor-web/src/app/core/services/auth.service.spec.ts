import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';
import { AuthService } from './auth.service';
import { AuthResponse } from '../models/auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  const baseAuthResponse: AuthResponse = {
    userId: 'user-1',
    token: 'fake-jwt-token',
    expiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(), // 1 time frem
    fullName: 'Test Testesen',
    email: 'test@test.dk',
    roles: ['User'],
  };

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('starter som ikke logget ind, når localStorage er tom', () => {
    expect(service.isLoggedIn()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('login() gemmer sessionen og sætter isLoggedIn() til sand', () => {
    service.login({ email: 'test@test.dk', password: 'Test1234!' }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/Auth/login'));
    expect(req.request.method).toBe('POST');
    req.flush(baseAuthResponse);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.currentUser()?.fullName).toBe('Test Testesen');
  });

  it('isLoggedIn() returnerer falsk, hvis token er udløbet', () => {
    const expiredResponse: AuthResponse = {
      ...baseAuthResponse,
      expiresAt: new Date(Date.now() - 60 * 1000).toISOString(), // 1 minut siden
    };

    service.login({ email: 'test@test.dk', password: 'Test1234!' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/Auth/login')).flush(expiredResponse);

    expect(service.isLoggedIn()).toBe(false);
  });

  it('isAdmin() er kun sand, når rollerne indeholder "Administrator"', () => {
    const adminResponse: AuthResponse = { ...baseAuthResponse, roles: ['User', 'Administrator'] };

    service.login({ email: 'admin@test.dk', password: 'Test1234!' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/Auth/login')).flush(adminResponse);

    expect(service.isAdmin()).toBe(true);
  });

  it('logout() rydder sessionen og navigerer til /login', () => {
    service.login({ email: 'test@test.dk', password: 'Test1234!' }).subscribe();
    httpMock.expectOne((r) => r.url.endsWith('/api/Auth/login')).flush(baseAuthResponse);

    const navigateSpy = vi.spyOn(router, 'navigate');
    service.logout();

    expect(service.isLoggedIn()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });
});