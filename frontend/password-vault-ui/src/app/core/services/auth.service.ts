import { Injectable, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { Observable, tap } from "rxjs";
import { environment } from "../../../environments/environment";

export interface UserDto {
  userId: number;
  fullName: string;
  email: string;
}
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly api = `${environment.apiUrl}/auth`;

  currentUser = signal<UserDto | null>(this.loadUser());

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

  register(
    fullName: string,
    email: string,
    password: string,
  ): Observable<ApiResponse<AuthResponse>> {
    return this.http
      .post<
        ApiResponse<AuthResponse>
      >(`${this.api}/register`, { fullName, email, password })
      .pipe(tap((r) => this.store(r.data)));
  }

  login(
    email: string,
    password: string,
  ): Observable<ApiResponse<AuthResponse>> {
    return this.http
      .post<ApiResponse<AuthResponse>>(`${this.api}/login`, { email, password })
      .pipe(tap((r) => this.store(r.data)));
  }

  refreshToken(): Observable<ApiResponse<AuthResponse>> {
    const token = localStorage.getItem(environment.refreshTokenKey) ?? "";
    return this.http
      .post<
        ApiResponse<AuthResponse>
      >(`${this.api}/refresh-token`, { refreshToken: token })
      .pipe(tap((r) => this.store(r.data)));
  }

  logout(): void {
    const token = localStorage.getItem(environment.refreshTokenKey) ?? "";
    this.http.post(`${this.api}/logout`, { refreshToken: token }).subscribe();
    localStorage.removeItem(environment.tokenKey);
    localStorage.removeItem(environment.refreshTokenKey);
    localStorage.removeItem(environment.userKey);
    this.currentUser.set(null);
    this.router.navigate(["/auth/login"]);
  }

  getToken(): string | null {
    return localStorage.getItem(environment.tokenKey);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  private store(auth: AuthResponse): void {
    localStorage.setItem(environment.tokenKey, auth.accessToken);
    localStorage.setItem(environment.refreshTokenKey, auth.refreshToken);
    localStorage.setItem(environment.userKey, JSON.stringify(auth.user));
    this.currentUser.set(auth.user);
  }

  private loadUser(): UserDto | null {
    const raw = localStorage.getItem(environment.userKey);
    return raw ? JSON.parse(raw) : null;
  }
}
