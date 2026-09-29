using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Khyout.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "otp_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    purpose = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    attempts_made = table.Column<short>(type: "smallint", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_codes", x => x.id);
                    table.CheckConstraint("ck_otp_codes_attempts", "attempts_made >= 0");
                    table.CheckConstraint("ck_otp_codes_purpose", "purpose IN ('Login', 'Onboarding')");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    target_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    attempts = table.Column<short>(type: "smallint", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_attempts", "attempts >= 0");
                    table.CheckConstraint("ck_outbox_messages_status", "status IN ('Pending', 'Sent', 'Failed')");
                    table.CheckConstraint("ck_outbox_messages_type", "type IN ('RfqCreated', 'QuotationSubmitted', 'QuotationAccepted', 'QuotationRejected', 'QuotationExpired', 'SampleRequested', 'SampleStatusChanged')");
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    logo_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.CheckConstraint("ck_companies_type", "type IN ('Buyer', 'Supplier')");
                    table.CheckConstraint("ck_companies_verification_status", "verification_status IN ('Pending', 'Verified', 'Rejected')");
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    moq = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    indicative_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_moq", "moq > 0");
                    table.CheckConstraint("ck_products_status", "status IN ('Draft', 'Active', 'Archived')");
                    table.CheckConstraint("ck_products_unit", "unit_of_measure IN ('Meter', 'Kg', 'Roll', 'Yard')");
                    table.ForeignKey(
                        name: "FK_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_companies_supplier_company_id",
                        column: x => x.supplier_company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN ('Buyer', 'Supplier', 'Admin')");
                    table.ForeignKey(
                        name: "FK_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fabric_attributes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gsm = table.Column<int>(type: "integer", nullable: false),
                    gsm_tolerance_pct = table.Column<int>(type: "integer", nullable: false),
                    weave_structure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    width_cm = table.Column<int>(type: "integer", nullable: false),
                    color_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    weight_per_meter_g = table.Column<int>(type: "integer", nullable: true),
                    care_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fabric_attributes", x => x.id);
                    table.CheckConstraint("ck_fabric_attributes_gsm", "gsm > 0");
                    table.CheckConstraint("ck_fabric_attributes_gsm_tolerance", "gsm_tolerance_pct BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_fabric_attributes_weave", "weave_structure IN ('Plain', 'Twill', 'Satin', 'Knit', 'Denim')");
                    table.CheckConstraint("ck_fabric_attributes_width", "width_cm > 0");
                    table.ForeignKey(
                        name: "FK_fabric_attributes_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fabric_composition",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiber_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fabric_composition", x => x.id);
                    table.CheckConstraint("ck_fabric_composition_percentage", "percentage BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_fabric_composition_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    width_px = table.Column<int>(type: "integer", nullable: false),
                    height_px = table.Column<int>(type: "integer", nullable: false),
                    size_bytes = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.CheckConstraint("ck_product_images_dimensions", "width_px > 0 AND height_px > 0 AND size_bytes > 0");
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq_quotations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_quotations", x => x.id);
                    table.CheckConstraint("ck_rfq_quotations_lead_time", "lead_time_days >= 0");
                    table.CheckConstraint("ck_rfq_quotations_price", "unit_price > 0");
                    table.CheckConstraint("ck_rfq_quotations_status", "status IN ('Submitted', 'Accepted', 'Rejected', 'Expired', 'Withdrawn')");
                    table.ForeignKey(
                        name: "FK_rfq_quotations_companies_supplier_company_id",
                        column: x => x.supplier_company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rfq_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    quantity_needed = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    closing_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    accepted_quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_requests", x => x.id);
                    table.CheckConstraint("ck_rfq_requests_quantity", "quantity_needed > 0");
                    table.CheckConstraint("ck_rfq_requests_status", "status IN ('Open', 'Awarded', 'Cancelled', 'Expired')");
                    table.ForeignKey(
                        name: "FK_rfq_requests_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfq_requests_companies_buyer_company_id",
                        column: x => x.buyer_company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rfq_requests_rfq_quotations_accepted_quotation_id",
                        column: x => x.accepted_quotation_id,
                        principalTable: "rfq_quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sample_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rfq_quotation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    delivery_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sample_requests", x => x.id);
                    table.CheckConstraint("ck_sample_requests_quantity", "quantity > 0");
                    table.CheckConstraint("ck_sample_requests_status", "status IN ('Requested', 'Approved', 'Shipped', 'Received', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_sample_requests_companies_buyer_company_id",
                        column: x => x.buyer_company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sample_requests_companies_supplier_company_id",
                        column: x => x.supplier_company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sample_requests_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sample_requests_rfq_quotations_rfq_quotation_id",
                        column: x => x.rfq_quotation_id,
                        principalTable: "rfq_quotations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "is_active", "name_ar", "name_en", "parent_id", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-000000000001"), true, "أقمشة قطنية", "Cotton Fabrics", null, "cotton-fabrics", 1 },
                    { new Guid("a1000000-0000-4000-8000-000000000002"), true, "أقمشة تريكو", "Knitted Fabrics", null, "knitted-fabrics", 2 },
                    { new Guid("a1000000-0000-4000-8000-000000000003"), true, "دنيم", "Denim", null, "denim", 3 },
                    { new Guid("a1000000-0000-4000-8000-000000000004"), true, "أقمشة صوفية", "Wool Fabrics", null, "wool-fabrics", 4 },
                    { new Guid("a1000000-0000-4000-8000-000000000005"), true, "أقمشة صناعية", "Synthetic Fabrics", null, "synthetic-fabrics", 5 },
                    { new Guid("a1000000-0000-4000-8000-000000000006"), true, "فيسكوز وحرير صناعي", "Viscose & Rayon", null, "viscose-rayon", 6 },
                    { new Guid("a1000000-0000-4000-8000-000000000007"), true, "أقمشة مخلوطة", "Blended Fabrics", null, "blended-fabrics", 7 },
                    { new Guid("a1000000-0000-4000-8000-000000000008"), true, "كتان", "Linen", null, "linen", 8 },
                    { new Guid("a1000000-0000-4000-8000-000000000009"), true, "خيوط قطنية", "Cotton Yarn", null, "cotton-yarn", 9 },
                    { new Guid("a1000000-0000-4000-8000-00000000000a"), true, "خيوط صناعية", "Synthetic Yarn", null, "synthetic-yarn", 10 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_sort",
                table: "categories",
                columns: new[] { "parent_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_city",
                table: "companies",
                column: "city");

            migrationBuilder.CreateIndex(
                name: "ix_companies_name",
                table: "companies",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_companies_verification_status",
                table: "companies",
                column: "verification_status");

            migrationBuilder.CreateIndex(
                name: "IX_companies_verified_by_user_id",
                table: "companies",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_fabric_attributes_gsm",
                table: "fabric_attributes",
                column: "gsm");

            migrationBuilder.CreateIndex(
                name: "ix_fabric_attributes_weave_structure",
                table: "fabric_attributes",
                column: "weave_structure");

            migrationBuilder.CreateIndex(
                name: "ux_fabric_attributes_product_id",
                table: "fabric_attributes",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fabric_composition_fiber_percentage",
                table: "fabric_composition",
                columns: new[] { "fiber_type", "percentage" });

            migrationBuilder.CreateIndex(
                name: "ux_fabric_composition_product_fiber",
                table: "fabric_composition",
                columns: new[] { "product_id", "fiber_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_expires_at",
                table: "otp_codes",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_otp_codes_lookup",
                table: "otp_codes",
                columns: new[] { "phone_number", "purpose", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_created_at",
                table: "outbox_messages",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_dispatch",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_sort",
                table: "product_images",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ux_product_images_content_hash",
                table: "product_images",
                column: "content_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_category_status_created",
                table: "products",
                columns: new[] { "category_id", "status", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_products_moq",
                table: "products",
                column: "moq");

            migrationBuilder.CreateIndex(
                name: "ix_products_supplier_company_id",
                table: "products",
                column: "supplier_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rfq_quotations_rfq_status",
                table: "rfq_quotations",
                columns: new[] { "rfq_request_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_rfq_quotations_supplier_created",
                table: "rfq_quotations",
                columns: new[] { "supplier_company_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_rfq_quotations_valid_until_status",
                table: "rfq_quotations",
                columns: new[] { "valid_until", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_rfq_quotations_rfq_supplier",
                table: "rfq_quotations",
                columns: new[] { "rfq_request_id", "supplier_company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rfq_requests_accepted_quotation_id",
                table: "rfq_requests",
                column: "accepted_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_rfq_requests_buyer_created",
                table: "rfq_requests",
                columns: new[] { "buyer_company_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_rfq_requests_category_status",
                table: "rfq_requests",
                columns: new[] { "category_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_rfq_requests_status_closing",
                table: "rfq_requests",
                columns: new[] { "status", "closing_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sample_requests_buyer_created",
                table: "sample_requests",
                columns: new[] { "buyer_company_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_sample_requests_product_id",
                table: "sample_requests",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sample_requests_rfq_quotation_id",
                table: "sample_requests",
                column: "rfq_quotation_id");

            migrationBuilder.CreateIndex(
                name: "ix_sample_requests_supplier_status",
                table: "sample_requests",
                columns: new[] { "supplier_company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_users_company_id",
                table: "users",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ux_users_phone_number",
                table: "users",
                column: "phone_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_companies_users_verified_by_user_id",
                table: "companies",
                column: "verified_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rfq_quotations_rfq_requests_rfq_request_id",
                table: "rfq_quotations",
                column: "rfq_request_id",
                principalTable: "rfq_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_companies_users_verified_by_user_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_rfq_requests_categories_category_id",
                table: "rfq_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_rfq_quotations_companies_supplier_company_id",
                table: "rfq_quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_rfq_requests_companies_buyer_company_id",
                table: "rfq_requests");

            migrationBuilder.DropForeignKey(
                name: "FK_rfq_quotations_rfq_requests_rfq_request_id",
                table: "rfq_quotations");

            migrationBuilder.DropTable(
                name: "fabric_attributes");

            migrationBuilder.DropTable(
                name: "fabric_composition");

            migrationBuilder.DropTable(
                name: "otp_codes");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "sample_requests");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "rfq_requests");

            migrationBuilder.DropTable(
                name: "rfq_quotations");
        }
    }
}
