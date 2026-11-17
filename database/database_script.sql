-- 1. Создание таблицы Ролей
CREATE TABLE Roles
(
    role_id INT IDENTITY(1,1) PRIMARY KEY,
    role_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ЗАПОЛНЕНИЕ РОЛЕЙ
INSERT INTO Roles (role_name)
VALUES (N'Администратор'),
       (N'Менеджер'),
       (N'Клиент');
GO


-- 2. Создание таблицы Типов объектов
CREATE TABLE PropertyTypes
(
    type_id INT IDENTITY(1,1) PRIMARY KEY,
    type_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ЗАПОЛНЕНИЕ ТИПОВ ОБЪЕКТОВ
INSERT INTO PropertyTypes (type_name)
VALUES (N'Квартира'),
       (N'Дом'),
       (N'Офис');
GO


-- 3. Создание таблицы Статусов объектов
CREATE TABLE PropertyStatuses
(
    status_id INT IDENTITY(1,1) PRIMARY KEY,
    status_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ЗАПОЛНЕНИЕ СТАТУСОВ ОБЪЕКТОВ
INSERT INTO PropertyStatuses (status_name)
VALUES (N'В продаже'),
       (N'Сдаётся'),
       (N'Продан'),
       (N'Сдан');
GO


-- 4. Создание таблицы Типов заявок
CREATE TABLE RequestTypes
(
    type_id INT IDENTITY(1,1) PRIMARY KEY,
    type_name NVARCHAR(50) NOT NULL UNIQUE,
    commission_rate DECIMAL(5,2) NOT NULL CHECK (commission_rate >= 0 AND commission_rate <= 100)
);
GO

-- ЗАПОЛНЕНИЕ ТИПОВ ЗАЯВОК
INSERT INTO RequestTypes (type_name, commission_rate)
VALUES (N'Покупка', 3.00),
       (N'Аренда', 50.00),
       (N'Просмотр', 0.00);
GO


-- 5. Создание таблицы Статусов заявок
CREATE TABLE RequestStatuses
(
    status_id INT IDENTITY(1,1) PRIMARY KEY,
    status_name NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- ЗАПОЛНЕНИЕ СТАТУСОВ ЗАЯВОК
INSERT INTO RequestStatuses (status_name)
VALUES (N'Новая'),
       (N'В работе'),
       (N'Завершена'),
       (N'Отменена');
GO


-- 6. Создание таблицы Пользователей
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    login NVARCHAR(50) NOT NULL UNIQUE,
    password NVARCHAR(50) NOT NULL,
    last_name NVARCHAR(60) NOT NULL,
    first_name NVARCHAR(60) NOT NULL,
    middle_name NVARCHAR(60),
    phone NVARCHAR(20) NOT NULL UNIQUE,
    email NVARCHAR(120) NOT NULL UNIQUE,
    role_id INT NOT NULL REFERENCES Roles(role_id),
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);
GO

-- ЗАПОЛНЕНИЕ ПОЛЬЗОВАТЕЛЕЙ
INSERT INTO Users (login, password, last_name, first_name, middle_name, phone, email, role_id) VALUES
(N'admin', N'123', N'Орлов', N'Андрей', N'Викторович', N'+79110000001', N'admin@estate.ru', 1),
(N'manager_petrova', N'123', N'Петрова', N'Анна', N'Сергеевна', N'+79110000002', N'petrova@estate.ru', 2),
(N'manager_sokolov', N'123', N'Соколов', N'Дмитрий', N'Олегович', N'+79110000003', N'sokolov@estate.ru', 2),
(N'client_ivanov', N'123', N'Иванов', N'Иван', N'Петрович', N'+79110000004', N'ivanov@mail.ru', 3);
GO


-- 7. Создание таблицы Объектов
CREATE TABLE Properties (
    property_id INT IDENTITY(1,1) PRIMARY KEY,
    property_code NVARCHAR(20) NOT NULL UNIQUE,
    title NVARCHAR(150) NOT NULL,
    address NVARCHAR(250) NOT NULL UNIQUE,
    type_id INT NOT NULL REFERENCES PropertyTypes(type_id),
    status_id INT NOT NULL REFERENCES PropertyStatuses(status_id),
    manager_id INT NOT NULL REFERENCES Users(user_id),
    price DECIMAL(14,2) NOT NULL CHECK (price > 0),
    area DECIMAL(8,2) NOT NULL CHECK (area > 0),
    rooms INT NOT NULL CHECK (rooms >= 0),
    free_viewings INT NOT NULL DEFAULT 0 CHECK (free_viewings >= 0),
    description NVARCHAR(MAX),
    image_path NVARCHAR(150),
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);
GO

-- ЗАПОЛНЕНИЕ ОБЪЕКТОВ
INSERT INTO Properties (property_code, title, address, type_id, status_id, manager_id, price, area, rooms, free_viewings, description, image_path) VALUES
(N'KV-101', N'Двухкомнатная квартира у метро', N'Санкт-Петербург, Учебная ул., д. 12, кв. 45', 1, 1, 2, 8500000.00, 54.50, 2, 8, N'Светлая квартира после ремонта, окна во двор, рядом метро и школа.', N'KV-101.png'),
(N'KV-102', N'Студия в новом доме', N'Санкт-Петербург, Учебная ул., д. 30, кв. 7', 1, 1, 2, 5200000.00, 27.00, 0, 6, N'Студия с чистовой отделкой, дом сдан в прошлом году, есть кладовая.', N'KV-102.png'),
(N'KV-103', N'Трёхкомнатная семейная квартира', N'Санкт-Петербург, Примерный пр., д. 8, кв. 112', 1, 1, 3, 12400000.00, 86.20, 3, 5, N'Просторная квартира с раздельными комнатами и двумя балконами.', N'KV-103.png'),
(N'KV-201', N'Однокомнатная квартира в аренду', N'Санкт-Петербург, Примерный пр., д. 21, кв. 16', 1, 2, 3, 42000.00, 38.00, 1, 4, N'Сдаётся на длительный срок, мебель и техника остаются.', N'KV-201.png'),
(N'KV-202', N'Двухкомнатная квартира в аренду', N'Санкт-Петербург, Тестовая наб., д. 3, кв. 58', 1, 2, 2, 65000.00, 61.00, 2, 3, N'Вид на набережную, консьерж, парковка во дворе.', N'KV-202.png'),
(N'DM-301', N'Загородный дом с участком', N'Ленинградская обл., пос. Условный, Лесная ул., д. 5', 2, 1, 3, 15800000.00, 148.00, 5, 2, N'Двухэтажный дом, участок 10 соток, газ и центральная вода.', N'DM-301.png'),
(N'DM-302', N'Небольшой дачный дом', N'Ленинградская обл., пос. Условный, Садовая ул., д. 17', 2, 1, 2, 4300000.00, 72.00, 3, 1, N'Каркасный дом для сезонного проживания, участок 6 соток.', NULL),
(N'OF-401', N'Офис в бизнес-центре', N'Санкт-Петербург, Деловая ул., д. 9, оф. 310', 3, 2, 3, 120000.00, 74.00, 3, 7, N'Офис класса B, отдельный вход с этажа, две переговорные.', N'OF-401.png'),
(N'OF-402', N'Помещение свободного назначения', N'Санкт-Петербург, Деловая ул., д. 15, пом. 2Н', 3, 1, 2, 19900000.00, 130.00, 4, 0, N'Первый этаж, отдельный вход, витринные окна.', N'OF-402.png'),
(N'KV-104', N'Проданная квартира', N'Санкт-Петербург, Учебная ул., д. 2, кв. 3', 1, 3, 2, 7100000.00, 44.00, 2, 3, N'Сделка завершена, объект остаётся в каталоге для истории.', N'KV-104.png');
GO


-- 8. Создание таблицы Заявок
CREATE TABLE Requests (
    request_id INT IDENTITY(1,1) PRIMARY KEY,
    client_id INT NOT NULL REFERENCES Users(user_id),
    property_id INT NOT NULL REFERENCES Properties(property_id),
    type_id INT NOT NULL REFERENCES RequestTypes(type_id),
    status_id INT NOT NULL REFERENCES RequestStatuses(status_id),
    commission_amount DECIMAL(14,2) NOT NULL CHECK (commission_amount >= 0),
    notes NVARCHAR(MAX),
    created_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    updated_at DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);
GO


-- 9. Создание таблицы Показов
CREATE TABLE Viewings (
    viewing_id INT IDENTITY(1,1) PRIMARY KEY,
    request_id INT NOT NULL REFERENCES Requests(request_id) ON DELETE CASCADE,
    manager_id INT NOT NULL REFERENCES Users(user_id),
    scheduled_at DATETIME2(0) NOT NULL,
    actual_start_at DATETIME2(0),
    actual_end_at DATETIME2(0),
    result NVARCHAR(100),
    feedback NVARCHAR(MAX),
    CONSTRAINT uq_viewings_manager_time UNIQUE (manager_id, scheduled_at),
    CONSTRAINT chk_viewings_actual CHECK (actual_end_at IS NULL OR actual_start_at IS NULL OR actual_end_at >= actual_start_at)
);
GO


-- Индекс для поиска заявок по объекту и дате
CREATE INDEX ix_request_property_date ON Requests (property_id, created_at);
GO
