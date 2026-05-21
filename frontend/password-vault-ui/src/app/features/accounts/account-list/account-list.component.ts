import { Component, OnInit, OnDestroy, signal, computed } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { RouterModule, Router } from "@angular/router";
import { MatTableModule } from "@angular/material/table";
import { MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatSortModule, Sort } from "@angular/material/sort";
import { MatInputModule } from "@angular/material/input";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatChipsModule } from "@angular/material/chips";
import { MatMenuModule } from "@angular/material/menu";
import { MatDialogModule, MatDialog } from "@angular/material/dialog";
import { debounceTime, Subject, takeUntil } from "rxjs";
import {
  AccountService,
  AccountDto,
} from "../../../core/services/account.service";
import { ToastService } from "../../../core/services/toast.service";
import { ConfirmDialogComponent } from "../../../shared/components/confirm-dialog/confirm-dialog.component";
import { environment } from "../../../../environments/environment";

interface RowState {
  revealing: boolean;
  revealed: boolean;
  password: string | null;
  timer?: ReturnType<typeof setTimeout>;
}

@Component({
  selector: "app-account-list",
  templateUrl: "./account-list.component.html",
  styleUrls: ["./account-list.component.css"],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatInputModule,
    MatFormFieldModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatChipsModule,
    MatMenuModule,
    MatDialogModule,
  ],
})
export class AccountListComponent implements OnInit, OnDestroy {
  columns = [
    "accountName",
    "userName",
    "category",
    "password",
    "createdDate",
    "actions",
  ];
  accounts: AccountDto[] = [];
  loading = false;
  searchTerm = "";
  categoryFilter = "";
  sortBy = "createdDate";
  sortDir = "desc";
  page = 1;
  pageSize = 20;
  totalCount = signal(0);

  categories = [
    "Social",
    "Banking",
    "Work",
    "Email",
    "Shopping",
    "Entertainment",
    "Other",
  ];

  // Per-row password reveal state
  private rowStates = new Map<number, RowState>();
  private search$ = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private accountSvc: AccountService,
    private toast: ToastService,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => this.loadData());
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.rowStates.forEach((s) => {
      if (s.timer) clearTimeout(s.timer);
    });
  }

  loadData(): void {
    this.loading = true;
    this.accountSvc
      .getAccounts({
        search: this.searchTerm || undefined,
        category: this.categoryFilter || undefined,
        sortBy: this.sortBy,
        sortDir: this.sortDir,
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (r) => {
          this.accounts = r.data.items;
          this.totalCount.set(r.data.totalCount);
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error("Failed to load accounts.");
        },
      });
  }

  onSearch(val: string): void {
    this.page = 1;
    this.search$.next(val);
  }
  clearSearch(): void {
    this.searchTerm = "";
    this.onSearch("");
  }

  onSort(s: Sort): void {
    this.sortBy = s.active;
    this.sortDir = s.direction || "desc";
    this.loadData();
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.loadData();
  }

  rowState(accountId: number): RowState | undefined {
    return this.rowStates.get(accountId);
  }

  toggleReveal(row: AccountDto): void {
    const state = this.rowStates.get(row.accountId);
    if (state?.revealed) {
      if (state.timer) clearTimeout(state.timer);
      this.rowStates.set(row.accountId, {
        ...state,
        revealed: false,
        password: null,
      });
      return;
    }

    this.rowStates.set(row.accountId, {
      revealing: true,
      revealed: false,
      password: null,
    });
    this.accountSvc.revealPassword(row.accountId).subscribe({
      next: async (r) => {
        const pwd = await this.accountSvc.decryptPassword(r.data);
        const timer = setTimeout(() => {
          this.rowStates.set(row.accountId, {
            revealing: false,
            revealed: false,
            password: null,
          });
        }, environment.passwordRevealTimeout);
        this.rowStates.set(row.accountId, {
          revealing: false,
          revealed: true,
          password: pwd,
          timer,
        });
      },
      error: () => {
        this.rowStates.set(row.accountId, {
          revealing: false,
          revealed: false,
          password: null,
        });
        this.toast.error("Could not decrypt password.");
      },
    });
  }

  copyPassword(row: AccountDto): void {
    this.accountSvc.revealPassword(row.accountId).subscribe({
      next: async (r) => {
        const pwd = await this.accountSvc.decryptPassword(r.data);
        navigator.clipboard.writeText(pwd);
        this.toast.success("Password copied!");
      },
      error: () => this.toast.error("Could not copy password."),
    });
  }

  copy(text: string, label: string): void {
    navigator.clipboard.writeText(text);
    this.toast.success(`${label} copied!`);
  }

  delete(row: AccountDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: "Delete Account",
        message: `Delete "${row.accountName}"? This cannot be undone.`,
      },
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.accountSvc.deleteAccount(row.accountId).subscribe({
        next: () => {
          this.toast.success("Account deleted.");
          this.loadData();
        },
        error: () => this.toast.error("Failed to delete account."),
      });
    });
  }
}
