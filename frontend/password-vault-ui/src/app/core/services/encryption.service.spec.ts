import { TestBed } from "@angular/core/testing";
import { EncryptionService } from "./encryption.service";

describe("EncryptionService", () => {
  let service: EncryptionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EncryptionService);
  });

  it("should be created", () => {
    expect(service).toBeTruthy();
  });

  it("should encrypt and decrypt round-trip correctly", async () => {
    const plainText = "MySecretPassword123!";
    const { cipherText, iv } = await service.encrypt(plainText);
    const decrypted = await service.decrypt(cipherText, iv);
    expect(decrypted).toBe(plainText);
  });

  it("should produce different ciphertext for same input (random IV)", async () => {
    const plainText = "SamePassword";
    const result1 = await service.encrypt(plainText);
    const result2 = await service.encrypt(plainText);
    expect(result1.iv).not.toBe(result2.iv);
    expect(result1.cipherText).not.toBe(result2.cipherText);
  });

  it("should handle empty string", async () => {
    const { cipherText, iv } = await service.encrypt("");
    const decrypted = await service.decrypt(cipherText, iv);
    expect(decrypted).toBe("");
  });

  it("should handle unicode characters", async () => {
    const text = "密码123 Пароль!@#";
    const { cipherText, iv } = await service.encrypt(text);
    const decrypted = await service.decrypt(cipherText, iv);
    expect(decrypted).toBe(text);
  });
});
