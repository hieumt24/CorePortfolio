---
name: plan-new-features
description: >-
  Đọc source code project CorePortfolio, hiểu architecture/domain hiện tại, và tự động lên ý tưởng kế hoạch implement các tính năng mới theo chuẩn đã định. Trình bày ý tưởng dưới dạng gạch đầu dòng trực tiếp trong cuộc hội thoại.
---

# Plan New Features

## Overview

Skill này hỗ trợ AI tự động đọc cấu trúc source code của dự án `CorePortfolio`, phân tích các domain và feature hiện có, đồng thời đối chiếu với các rules bắt buộc trong `AGENTS.md` để lên ý tưởng và viết kế hoạch chi tiết cho các tính năng mới. Không yêu cầu sử dụng script bổ trợ, workflow hoàn toàn dựa vào khả năng reasoning và đọc file của agent.

## Dependencies

- **`AGENTS.md`**: File chứa các quy tắc thiết kế chung của project (nhất thiết phải đọc và tuân thủ).
- **`coreportfolio-fe-architecture`** (nếu liên quan tới Frontend).
- **`coreportfolio-react-best-practices`** (nếu liên quan tới Frontend).

## Quick Start

Bạn chỉ cần ra lệnh cho AI như sau:
- "Lên ý tưởng cho tính năng quản lý danh mục đầu tư bằng skill plan-new-features."
- "Dùng plan-new-features để gợi ý thêm một vài chức năng backend mới cho module Cashflow."

## Workflow

Thực hiện tuần tự các bước sau khi được user yêu cầu lên ý tưởng tính năng mới:

### 1. Thu thập Context (Đọc codebase)
- Sử dụng các công cụ đọc file (`view_file`, `list_dir`, `grep_search`) để tìm hiểu kiến trúc hiện tại của dự án.
- **Backend:** Tập trung quét thư mục lõi như `CorePortfolio.API/Features` hoặc `src/` (nếu có thay đổi cấu trúc).
- **Frontend:** Tập trung quét thư mục các components hoặc màn hình chính.
- *Lưu ý:* Nếu codebase lớn, chỉ focus vào khu vực liên quan nhất tới chủ đề cần lên ý tưởng.

### 2. Đọc Rules & Constraints
- Đọc file `AGENTS.md` tại root của workspace để lấy các rule bắt buộc (ví dụ: Backend phải sử dụng Vertical Slice Architecture, MediatR, Minimal APIs; tuyệt đối không dùng MVC Controllers).
- Tuân thủ các file rules khác nếu được chỉ định. Bất kỳ ý tưởng nào đề xuất ra cũng phải bám sát các rule này.

### 3. Lên ý tưởng (Ideation)
- Tự động gợi ý hướng đi hoặc mở rộng ý tưởng dựa trên keyword user đưa ra.
- Chia nhỏ tính năng ra thành các đầu mục rõ ràng: Cấu trúc thư mục (Folder structure), Backend (Command/Query/Endpoints), Frontend (Components/UI).

### 4. Viết Output
- Trình bày kế hoạch trực tiếp trong cửa sổ chat (conversation).
- Trình bày dưới dạng danh sách gạch đầu dòng (bullet points) rõ ràng, ngắn gọn và dễ hiểu.
- KHÔNG CẦN lưu lại dưới dạng file `.md`.

## Common Mistakes

- **Bỏ quên Rules:** Không đọc hoặc phớt lờ các kiến trúc bắt buộc trong `AGENTS.md` (ví dụ, đề xuất tạo một file Controller kiểu MVC trong project dùng Minimal API).
- **Lưu file Markdown:** Cố gắng lưu kế hoạch vào thư mục `docs/features` thay vì phản hồi trực tiếp cho user.
- **Trình bày quá dài dòng:** Viết những đoạn văn quá dài thay vì sử dụng gạch đầu dòng (bullet points) ngắn gọn theo convention.
