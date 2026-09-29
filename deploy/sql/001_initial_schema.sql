CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE categories (
        id uuid NOT NULL,
        slug character varying(100) NOT NULL,
        name_ar character varying(200) NOT NULL,
        name_en character varying(200) NOT NULL,
        parent_id uuid,
        sort_order integer NOT NULL,
        is_active boolean NOT NULL,
        CONSTRAINT "PK_categories" PRIMARY KEY (id),
        CONSTRAINT "FK_categories_categories_parent_id" FOREIGN KEY (parent_id) REFERENCES categories (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE otp_codes (
        id uuid NOT NULL,
        phone_number character varying(20) NOT NULL,
        purpose character varying(20) NOT NULL,
        code_hash character varying(128) NOT NULL,
        attempts_made smallint NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        consumed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_otp_codes" PRIMARY KEY (id),
        CONSTRAINT ck_otp_codes_attempts CHECK (attempts_made >= 0),
        CONSTRAINT ck_otp_codes_purpose CHECK (purpose IN ('Login', 'Onboarding'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        type character varying(30) NOT NULL,
        payload jsonb NOT NULL,
        target_ref character varying(100) NOT NULL,
        attempts smallint NOT NULL,
        next_attempt_at timestamp with time zone NOT NULL,
        status character varying(20) NOT NULL,
        last_error character varying(1000),
        created_at timestamp with time zone NOT NULL,
        sent_at timestamp with time zone,
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT ck_outbox_messages_attempts CHECK (attempts >= 0),
        CONSTRAINT ck_outbox_messages_status CHECK (status IN ('Pending', 'Sent', 'Failed')),
        CONSTRAINT ck_outbox_messages_type CHECK (type IN ('RfqCreated', 'QuotationSubmitted', 'QuotationAccepted', 'QuotationRejected', 'QuotationExpired', 'SampleRequested', 'SampleStatusChanged'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE companies (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        type character varying(20) NOT NULL,
        city character varying(100) NOT NULL,
        address character varying(400),
        bio text,
        verification_status character varying(20) NOT NULL,
        verified_at timestamp with time zone,
        verified_by_user_id uuid,
        logo_path character varying(300),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_companies" PRIMARY KEY (id),
        CONSTRAINT ck_companies_type CHECK (type IN ('Buyer', 'Supplier')),
        CONSTRAINT ck_companies_verification_status CHECK (verification_status IN ('Pending', 'Verified', 'Rejected'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE products (
        id uuid NOT NULL,
        supplier_company_id uuid NOT NULL,
        category_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        description text,
        moq numeric(14,2) NOT NULL,
        unit_of_measure character varying(20) NOT NULL,
        indicative_price numeric(14,2),
        currency character varying(3),
        status character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_products" PRIMARY KEY (id),
        CONSTRAINT ck_products_moq CHECK (moq > 0),
        CONSTRAINT ck_products_status CHECK (status IN ('Draft', 'Active', 'Archived')),
        CONSTRAINT ck_products_unit CHECK (unit_of_measure IN ('Meter', 'Kg', 'Roll', 'Yard')),
        CONSTRAINT "FK_products_categories_category_id" FOREIGN KEY (category_id) REFERENCES categories (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_products_companies_supplier_company_id" FOREIGN KEY (supplier_company_id) REFERENCES companies (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        phone_number character varying(20) NOT NULL,
        full_name character varying(200) NOT NULL,
        role character varying(20) NOT NULL,
        company_id uuid,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY (id),
        CONSTRAINT ck_users_role CHECK (role IN ('Buyer', 'Supplier', 'Admin')),
        CONSTRAINT "FK_users_companies_company_id" FOREIGN KEY (company_id) REFERENCES companies (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE fabric_attributes (
        id uuid NOT NULL,
        product_id uuid NOT NULL,
        gsm integer NOT NULL,
        gsm_tolerance_pct integer NOT NULL,
        weave_structure character varying(20) NOT NULL,
        width_cm integer NOT NULL,
        color_family character varying(50),
        weight_per_meter_g integer,
        care_notes character varying(1000),
        CONSTRAINT "PK_fabric_attributes" PRIMARY KEY (id),
        CONSTRAINT ck_fabric_attributes_gsm CHECK (gsm > 0),
        CONSTRAINT ck_fabric_attributes_gsm_tolerance CHECK (gsm_tolerance_pct BETWEEN 0 AND 100),
        CONSTRAINT ck_fabric_attributes_weave CHECK (weave_structure IN ('Plain', 'Twill', 'Satin', 'Knit', 'Denim')),
        CONSTRAINT ck_fabric_attributes_width CHECK (width_cm > 0),
        CONSTRAINT "FK_fabric_attributes_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE fabric_composition (
        id uuid NOT NULL,
        product_id uuid NOT NULL,
        fiber_type character varying(30) NOT NULL,
        percentage numeric(5,2) NOT NULL,
        CONSTRAINT "PK_fabric_composition" PRIMARY KEY (id),
        CONSTRAINT ck_fabric_composition_percentage CHECK (percentage BETWEEN 0 AND 100),
        CONSTRAINT "FK_fabric_composition_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE product_images (
        id uuid NOT NULL,
        product_id uuid NOT NULL,
        storage_path character varying(300) NOT NULL,
        content_hash character varying(64) NOT NULL,
        width_px integer NOT NULL,
        height_px integer NOT NULL,
        size_bytes integer NOT NULL,
        sort_order smallint NOT NULL,
        CONSTRAINT "PK_product_images" PRIMARY KEY (id),
        CONSTRAINT ck_product_images_dimensions CHECK (width_px > 0 AND height_px > 0 AND size_bytes > 0),
        CONSTRAINT "FK_product_images_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(128) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        replaced_by_token_id uuid,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE rfq_quotations (
        id uuid NOT NULL,
        rfq_request_id uuid NOT NULL,
        supplier_company_id uuid NOT NULL,
        unit_price numeric(14,2) NOT NULL,
        currency character varying(3) NOT NULL,
        valid_until timestamp with time zone NOT NULL,
        lead_time_days integer NOT NULL,
        note character varying(2000),
        status character varying(20) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_rfq_quotations" PRIMARY KEY (id),
        CONSTRAINT ck_rfq_quotations_lead_time CHECK (lead_time_days >= 0),
        CONSTRAINT ck_rfq_quotations_price CHECK (unit_price > 0),
        CONSTRAINT ck_rfq_quotations_status CHECK (status IN ('Submitted', 'Accepted', 'Rejected', 'Expired', 'Withdrawn')),
        CONSTRAINT "FK_rfq_quotations_companies_supplier_company_id" FOREIGN KEY (supplier_company_id) REFERENCES companies (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE rfq_requests (
        id uuid NOT NULL,
        buyer_company_id uuid NOT NULL,
        category_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        description character varying(4000),
        quantity_needed numeric(14,2) NOT NULL,
        unit_of_measure character varying(20) NOT NULL,
        target_delivery_date date NOT NULL,
        closing_date timestamp with time zone NOT NULL,
        status character varying(20) NOT NULL,
        accepted_quotation_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_rfq_requests" PRIMARY KEY (id),
        CONSTRAINT ck_rfq_requests_quantity CHECK (quantity_needed > 0),
        CONSTRAINT ck_rfq_requests_status CHECK (status IN ('Open', 'Awarded', 'Cancelled', 'Expired')),
        CONSTRAINT "FK_rfq_requests_categories_category_id" FOREIGN KEY (category_id) REFERENCES categories (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rfq_requests_companies_buyer_company_id" FOREIGN KEY (buyer_company_id) REFERENCES companies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_rfq_requests_rfq_quotations_accepted_quotation_id" FOREIGN KEY (accepted_quotation_id) REFERENCES rfq_quotations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE TABLE sample_requests (
        id uuid NOT NULL,
        buyer_company_id uuid NOT NULL,
        supplier_company_id uuid NOT NULL,
        product_id uuid NOT NULL,
        rfq_quotation_id uuid,
        status character varying(20) NOT NULL,
        quantity numeric(14,2) NOT NULL,
        delivery_city character varying(100),
        note character varying(1000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_sample_requests" PRIMARY KEY (id),
        CONSTRAINT ck_sample_requests_quantity CHECK (quantity > 0),
        CONSTRAINT ck_sample_requests_status CHECK (status IN ('Requested', 'Approved', 'Shipped', 'Received', 'Rejected')),
        CONSTRAINT "FK_sample_requests_companies_buyer_company_id" FOREIGN KEY (buyer_company_id) REFERENCES companies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_sample_requests_companies_supplier_company_id" FOREIGN KEY (supplier_company_id) REFERENCES companies (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_sample_requests_products_product_id" FOREIGN KEY (product_id) REFERENCES products (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_sample_requests_rfq_quotations_rfq_quotation_id" FOREIGN KEY (rfq_quotation_id) REFERENCES rfq_quotations (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000001', TRUE, 'أقمشة قطنية', 'Cotton Fabrics', NULL, 'cotton-fabrics', 1);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000002', TRUE, 'أقمشة تريكو', 'Knitted Fabrics', NULL, 'knitted-fabrics', 2);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000003', TRUE, 'دنيم', 'Denim', NULL, 'denim', 3);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000004', TRUE, 'أقمشة صوفية', 'Wool Fabrics', NULL, 'wool-fabrics', 4);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000005', TRUE, 'أقمشة صناعية', 'Synthetic Fabrics', NULL, 'synthetic-fabrics', 5);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000006', TRUE, 'فيسكوز وحرير صناعي', 'Viscose & Rayon', NULL, 'viscose-rayon', 6);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000007', TRUE, 'أقمشة مخلوطة', 'Blended Fabrics', NULL, 'blended-fabrics', 7);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000008', TRUE, 'كتان', 'Linen', NULL, 'linen', 8);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-000000000009', TRUE, 'خيوط قطنية', 'Cotton Yarn', NULL, 'cotton-yarn', 9);
    INSERT INTO categories (id, is_active, name_ar, name_en, parent_id, slug, sort_order)
    VALUES ('a1000000-0000-4000-8000-00000000000a', TRUE, 'خيوط صناعية', 'Synthetic Yarn', NULL, 'synthetic-yarn', 10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_categories_parent_sort ON categories (parent_id, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_categories_slug ON categories (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_companies_city ON companies (city);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_companies_name ON companies (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_companies_verification_status ON companies (verification_status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX "IX_companies_verified_by_user_id" ON companies (verified_by_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_fabric_attributes_gsm ON fabric_attributes (gsm);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_fabric_attributes_weave_structure ON fabric_attributes (weave_structure);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_fabric_attributes_product_id ON fabric_attributes (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_fabric_composition_fiber_percentage ON fabric_composition (fiber_type, percentage);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_fabric_composition_product_fiber ON fabric_composition (product_id, fiber_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_otp_codes_expires_at ON otp_codes (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_otp_codes_lookup ON otp_codes (phone_number, purpose, expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_outbox_messages_created_at ON outbox_messages (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_outbox_messages_dispatch ON outbox_messages (status, next_attempt_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_product_images_product_sort ON product_images (product_id, sort_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_product_images_content_hash ON product_images (content_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_products_category_status_created ON products (category_id, status, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_products_moq ON products (moq);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_products_supplier_company_id ON products (supplier_company_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_expires_at ON refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_quotations_rfq_status ON rfq_quotations (rfq_request_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_quotations_supplier_created ON rfq_quotations (supplier_company_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_quotations_valid_until_status ON rfq_quotations (valid_until, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_rfq_quotations_rfq_supplier ON rfq_quotations (rfq_request_id, supplier_company_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX "IX_rfq_requests_accepted_quotation_id" ON rfq_requests (accepted_quotation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_requests_buyer_created ON rfq_requests (buyer_company_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_requests_category_status ON rfq_requests (category_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_rfq_requests_status_closing ON rfq_requests (status, closing_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_sample_requests_buyer_created ON sample_requests (buyer_company_id, created_at DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX "IX_sample_requests_product_id" ON sample_requests (product_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX "IX_sample_requests_rfq_quotation_id" ON sample_requests (rfq_quotation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_sample_requests_supplier_status ON sample_requests (supplier_company_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_users_company_id ON users (company_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE INDEX ix_users_role ON users (role);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    CREATE UNIQUE INDEX ux_users_phone_number ON users (phone_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    ALTER TABLE companies ADD CONSTRAINT "FK_companies_users_verified_by_user_id" FOREIGN KEY (verified_by_user_id) REFERENCES users (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    ALTER TABLE rfq_quotations ADD CONSTRAINT "FK_rfq_quotations_rfq_requests_rfq_request_id" FOREIGN KEY (rfq_request_id) REFERENCES rfq_requests (id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929074745_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260929074745_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

