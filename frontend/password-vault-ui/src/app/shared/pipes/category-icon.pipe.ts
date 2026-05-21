import { Pipe, PipeTransform } from "@angular/core";

/** Maps category to an icon name. */
@Pipe({ name: "categoryIcon", standalone: true })
export class CategoryIconPipe implements PipeTransform {
  private map: Record<string, string> = {
    Social: "people",
    Banking: "account_balance",
    Work: "work",
    Email: "email",
    Shopping: "shopping_cart",
    Entertainment: "movie",
    Other: "label",
  };

  transform(category: string | null): string {
    return this.map[category ?? ""] ?? "label";
  }
}
