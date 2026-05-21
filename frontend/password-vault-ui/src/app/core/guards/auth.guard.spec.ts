import { TestBed } from "@angular/core/testing";
import { Router } from "@angular/router";
import { authGuard, guestGuard } from "./auth.guard";
import { AuthService } from "../services/auth.service";
import { ActivatedRouteSnapshot, RouterStateSnapshot } from "@angular/router";

describe("authGuard", () => {
  let authServiceMock: jasmine.SpyObj<AuthService>;
  let routerMock: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authServiceMock = jasmine.createSpyObj("AuthService", ["isAuthenticated"]);
    routerMock = jasmine.createSpyObj("Router", ["navigate"]);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    });
  });

  it("should allow access when authenticated", () => {
    authServiceMock.isAuthenticated.and.returnValue(true);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(result).toBeTrue();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  it("should redirect to login when not authenticated", () => {
    authServiceMock.isAuthenticated.and.returnValue(false);
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    expect(result).toBeFalse();
    expect(routerMock.navigate).toHaveBeenCalledWith(["/auth/login"]);
  });
});
