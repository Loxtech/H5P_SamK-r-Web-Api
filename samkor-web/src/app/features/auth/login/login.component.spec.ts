import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('formularen er ugyldig, når felterne er tomme', () => {
    expect(component.form.valid).toBe(false);
  });

  it('e-mail-feltet afviser en ugyldig e-mailadresse', () => {
    const emailControl = component.form.controls.email;

    emailControl.setValue('ikke-en-email');
    expect(emailControl.valid).toBe(false);

    emailControl.setValue('test@test.dk');
    expect(emailControl.valid).toBe(true);
  });

  it('adgangskode-feltet er påkrævet', () => {
    const passwordControl = component.form.controls.password;

    expect(passwordControl.valid).toBe(false);

    passwordControl.setValue('hemmeligt');
    expect(passwordControl.valid).toBe(true);
  });

  it('formularen er gyldig, når begge felter er korrekt udfyldt', () => {
    component.form.setValue({ email: 'test@test.dk', password: 'Test1234!' });
    expect(component.form.valid).toBe(true);
  });

  it('submit() sender ikke noget og markerer felterne som touched, når formularen er ugyldig', () => {
    component.submit();

    expect(component.form.controls.email.touched).toBe(true);
    expect(component.form.controls.password.touched).toBe(true);
    expect(component.isSubmitting()).toBe(false);
  });
});