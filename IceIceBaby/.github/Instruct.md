# **DDS Ice Portal — ASP.NET MVC (Views Only) Technical Review**

---

## **1. What’s Solid**
- ✅ Clear MVP scope and flows for Orders, Delivery Runs, Invoicing, and Dashboard.  
- ✅ Practical data model baseline and role matrix.  
- ✅ Sensible storage paths for POD and invoices; PDF generation explicitly defined.  

---

## **2. Key Gaps and Risks**

| Area | Risk / Gap | Recommendation |
|------|-------------|----------------|
| **Framework alignment** | Solution will use MVC (Controllers + Views) only. Prior spec/workspace referenced Razor Pages. | Standardize on **ASP.NET Core 8 MVC (Views only)**. Remove Razor Pages endpoints/folders and templates. |
| **Payment audit** | Boolean `Paid` on Order lacks traceability. | Add **Payments** table with amount, method, timestamp, recorded by. |
| **Pricing integrity** | Unit prices change over time. | Snapshot **UnitPriceSnapshot** on `OrderItem`. |
| **Delivery run integrity** | An order can be added to multiple runs. | Enforce **unique constraint** on `DeliveryStop.OrderId`. |
| **File security** | POD stored under `wwwroot` exposes public URLs. | Randomize filenames; validate uploads; later move outside `wwwroot`. |
| **Concurrency** | Status changes and run closing not transactional. | Add `RowVersion` concurrency token; wrap updates in transactions. |
| **Stock rules** | Adjustments mentioned but no defined mutation flows. | Implement explicit endpoints/services for `StockDay` and `StockAdjust`. |

---

## **2.1 MVC Migration Notes (from Razor Pages, if applicable)**
- Remove Razor Pages registration in `Program.cs` and add MVC:
  - Use `builder.Services.AddControllersWithViews()`.
  - Configure routing with `app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");`.
  - Remove `app.MapRazorPages()`.
- Delete or archive the `Pages/` folder; create `Controllers/` and `Views/` structure.
- Ensure `_ViewImports.cshtml` and `_ViewStart.cshtml` exist under `Views/`.
- Update layout and partials in `Views/Shared/`.
- Replace page handler posts with controller actions (POST endpoints) and unobtrusive AJAX/HTMX as needed.

---

## **3. MVC Implementation Structure**

### **3.1 Controllers**
```
/Controllers
 ├─ OrdersController.cs       → CRUD, status transitions, invoice actions
 ├─ DeliveryRunsController.cs → Create run, assign orders, mark delivered
 ├─ DashboardController.cs    → Reports and metrics
 ├─ PaymentsController.cs     → Record payments, list transactions
 ├─ FilesController.cs        → POD upload/download endpoints
```

### **3.2 Views**
```
/Views
 ├─ Orders/
 │   ├─ Index.cshtml       → Orders list or board
 │   ├─ Create.cshtml      → Create order form
 │   ├─ Details.cshtml     → Order summary
 │   ├─ Invoice.cshtml     → PDF view or download
 ├─ DeliveryRuns/
 │   ├─ Index.cshtml       → List all runs
 │   ├─ Create.cshtml      → Build new run
 │   ├─ Details.cshtml     → Driver view (mark delivered + POD upload)
 ├─ Dashboard/
 │   ├─ Index.cshtml       → KPI dashboard
 ├─ Shared/
 │   ├─ _Layout.cshtml     → Bootstrap layout
 │   ├─ _Navbar.cshtml     → Role-based navigation
 │   ├─ _StatusPartial.cshtml → For status updates with AJAX
```

---

## **4. Services**

| Interface | Responsibility |
|------------|----------------|
| **IOrderService** | Create orders, subtotal calc, manage status transitions, trigger invoice generation. |
| **IRunService** | Create runs, enforce rules, close runs transactionally. |
| **IPdfService** | Generate invoices (QuestPDF or IronPDF). |
| **IStorageService** | Handle POD uploads (validate, randomize filename, save to storage). |
| **IEmailSender** | Email invoices or delivery confirmations. |

---

## **5. Identity & Authorization**

- Use **ASP.NET Core Identity** with seeded roles:  
  - `Manager`  
  - `Clerk`  
  - `Driver`  
  - `Viewer`  

- Apply **role-based authorization filters** on controllers/actions:  
  - `[Authorize(Roles = "Clerk,Manager")]` → Order operations  
  - `[Authorize(Roles = "Driver,Manager")]` → Delivery runs  
  - `[Authorize(Roles = "Manager")]` → Dashboard and settings  

---

## **6. Data Model (EF Core 8)**

### **Enums**
```csharp
public enum OrderStatus { New, Confirmed, OutForDelivery, Delivered, Canceled }
public enum DeliveryType { Delivery, Pickup }
public enum PaymentMethod { Cash, EFT }
```

### **Entities**

#### **Order**
- `Id` (PK)  
- `OrderNo` (Format: DDMMYY-###, sequential per day)  
- `CustomerId`  
- `DeliveryType`  
- `Status`  
- `Subtotal decimal(18,2)`  
- `PromisedAt DateTimeOffset`  
- `Notes`  
- `RowVersion byte[]` (for concurrency)  
- Optional minimal: `IsPaid`, `PaymentMethod?`, `PaidAt?`

#### **OrderItem**
- `ProductId`  
- `Qty`  
- `UnitPriceSnapshot decimal(18,2)`  
- `LineTotal decimal(18,2)`

#### **Payment**
- `Id`  
- `OrderId`  
- `Amount decimal(18,2)`  
- `Method`  
- `PaidAt DateTimeOffset`  
- `RecordedBy string`

#### **DeliveryRun**
- `Id`  
- `RunDate DateOnly`  
- `DriverName`  
- `Vehicle`  
- `Status`  
- `RowVersion byte[]`

#### **DeliveryStop**
- `Id`  
- `RunId`  
- `OrderId` (UNIQUE)  
- `Seq int`  
- `DeliveredAt DateTimeOffset?`  
- `PodNote`  
- `PodPhotoPath`

### **Indexes & Constraints**
- Unique index on `Order.OrderNo`  
- Unique index on `DeliveryStop.OrderId`  
- Indexes on `Order.Status`, `Order.PromisedAt`, `DeliveryRun.RunDate`  
- Configure decimal precision and concurrency tokens via Fluent API  

---

## **7. Business Rule Enforcement**

- **Run creation:** Must include at least one confirmed order.  
- **Order assignment:** Only confirmed orders can be attached to runs.  
- **Delivered lock:** Once delivered, order is immutable.  
- **Transactional safety:** `Run.Close()` wraps delivery updates and order status changes in one transaction.  
- **Stock sync:** Canceling an order **returns the amount to stock**; completing a delivery reduces available stock.  

---

## **8. Invoices and Payments**

- Generate PDFs using **QuestPDF** or **IronPDF** via `IPdfService`.  
- Use `OrderItem.UnitPriceSnapshot` for historical pricing accuracy.  
- Store generated file path under `/wwwroot/invoices/{yyyy}/{MM}/`.  
- **Payment workflow:**
  1. Create a record in the `Payment` table.  
  2. Mark `Order.IsPaid = true` when `sum(Payments) >= Subtotal`.  
  3. Keep all payment events for audit.  

---

## **9. File Uploads (Proof of Delivery)**

- Accept `IFormFile` with type and size validation (`image/jpeg`, `image/png`).  
- Randomize filenames before saving.  
- Save under `/wwwroot/pod/{yyyy}/{MM}/`.  
- For MVP, **no retention policy** is applied.  
- Optionally store outside `wwwroot` and serve via a secure endpoint later.  

---

## **10. Dashboard & Reporting**

- Aggregate daily metrics:
  - Orders created, confirmed, delivered, canceled  
  - Cash collected  
  - Total revenue  
  - Delivery success rate  
- Use LINQ projections and server-side pagination for large datasets.  
- Implement date filters using `DateOnly` for simplicity.  

---

## **11. Open Questions (Resolved)**

| Question | Decision |
|-----------|-----------|
| **OrderNo format?** | Sequential number format `DDMMYY-###` |
| **Per-line price overrides?** | Not included in MVP |
| **Canceled orders and stock impact?** | Return amount to stock |
| **Email sender setup?** | Configure SMTP later (post-MVP) |
| **POD image retention policy?** | None for MVP |

---

## **12. Actionable Next Steps**

1. Scaffold **Controllers** and **Views** listed above.  
2. Add **EF Core entities**, enums, and Fluent configuration; run migrations.  
3. Implement `IOrderService`, `IRunService`, `IPdfService`, `IStorageService`.  
4. Seed Identity roles (`Manager`, `Clerk`, `Driver`, `Viewer`).  
5. Apply `[Authorize]` policies to actions.  
6. Build Orders dashboard with AJAX/partial updates for fast transitions.  
7. Add tests for:  
   - Run creation validation  
   - Unique `OrderNo`  
   - Delivery lifecycle transitions  
   - Payment audit trail  

---

## **13. Summary**

The **ASP.NET MVC (Views only)** architecture ensures long-term scalability and testability while keeping the MVP lightweight.  
Orders, Runs, and Payments are the core transactional domains; Invoices and PODs provide the audit trail.  
Once stabilized, add forecasting, route optimization, and customer self-service as post-MVP features.
