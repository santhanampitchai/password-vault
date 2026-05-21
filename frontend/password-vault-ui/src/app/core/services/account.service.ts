import { Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { EncryptionService } from "./encryption.service";
import { ApiResponse } from "./auth.service";
import { switchMap } from "rxjs/operators";
import { from } from "rxjs";

export interface AccountDto {
  accountId: number;
  accountName: string;
  userName: string;
  category: string | null;
  websiteUrl: string | null;
  createdDate: string;
  updatedDate: string | null;
}

export interface AccountDetailDto extends AccountDto {
  encryptedPassword: string;
  clientIV: string;
  encryptedOtherInfo: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AccountQueryParams {
  search?: string;
  category?: string;
  sortBy?: string;
  sortDir?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateAccountPayload {
  accountName: string;
  userName: string;
  encryptedPassword: string;
  clientIV: string;
  encryptedOtherInfo: string | null;
  category: string | null;
  websiteUrl: string | null;
}

@Injectable({ providedIn: "root" })
export class AccountService {
  private readonly api = `${environment.apiUrl}/accounts`;

  constructor(
    private http: HttpClient,
    private encryption: EncryptionService,
  ) {}

  getAccounts(
    query: AccountQueryParams = {},
  ): Observable<ApiResponse<PagedResult<AccountDto>>> {
    let params = new HttpParams();
    if (query.search) params = params.set("search", query.search);
    if (query.category) params = params.set("category", query.category);
    if (query.sortBy) params = params.set("sortBy", query.sortBy);
    if (query.sortDir) params = params.set("sortDir", query.sortDir);
    if (query.page) params = params.set("page", query.page.toString());
    if (query.pageSize)
      params = params.set("pageSize", query.pageSize.toString());
    return this.http.get<ApiResponse<PagedResult<AccountDto>>>(this.api, {
      params,
    });
  }

  getAccount(id: number): Observable<ApiResponse<AccountDetailDto>> {
    return this.http.get<ApiResponse<AccountDetailDto>>(`${this.api}/${id}`);
  }

  /**
   * Encrypts password client-side, then posts to API.
   */
  createAccount(
    accountName: string,
    userName: string,
    password: string,
    otherInfo: string | null,
    category: string | null,
    websiteUrl: string | null,
  ): Observable<ApiResponse<AccountDetailDto>> {
    return from(this.encryptFields(password, otherInfo)).pipe(
      switchMap(({ encPwd, iv, encOther }) => {
        const payload: CreateAccountPayload = {
          accountName,
          userName,
          encryptedPassword: encPwd,
          clientIV: iv,
          encryptedOtherInfo: encOther,
          category,
          websiteUrl,
        };
        return this.http.post<ApiResponse<AccountDetailDto>>(this.api, payload);
      }),
    );
  }

  updateAccount(
    id: number,
    accountName: string,
    userName: string,
    password: string,
    otherInfo: string | null,
    category: string | null,
    websiteUrl: string | null,
  ): Observable<ApiResponse<AccountDetailDto>> {
    return from(this.encryptFields(password, otherInfo)).pipe(
      switchMap(({ encPwd, iv, encOther }) => {
        const payload: CreateAccountPayload = {
          accountName,
          userName,
          encryptedPassword: encPwd,
          clientIV: iv,
          encryptedOtherInfo: encOther,
          category,
          websiteUrl,
        };
        return this.http.put<ApiResponse<AccountDetailDto>>(
          `${this.api}/${id}`,
          payload,
        );
      }),
    );
  }

  deleteAccount(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/${id}`);
  }

  /**
   * Server unwraps its layer, returns client-encrypted payload.
   * We then decrypt client-side.
   */
  revealPassword(id: number): Observable<ApiResponse<AccountDetailDto>> {
    return this.http.post<ApiResponse<AccountDetailDto>>(
      `${this.api}/${id}/decrypt-password`,
      {},
    );
  }

  async decryptPassword(detail: AccountDetailDto): Promise<string> {
    return this.encryption.decrypt(detail.encryptedPassword, detail.clientIV);
  }

  private async encryptFields(
    password: string,
    otherInfo: string | null,
  ): Promise<{ encPwd: string; iv: string; encOther: string | null }> {
    const { cipherText: encPwd, iv } = await this.encryption.encrypt(password);
    let encOther: string | null = null;
    if (otherInfo) {
      const r = await this.encryption.encrypt(otherInfo);
      encOther = r.cipherText;
    }
    return { encPwd, iv, encOther };
  }
}
