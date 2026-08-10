# PosSetupManager 디자인 스펙

이 파일을 수정하면 `FluentHelper.cs`에 반영해드립니다.

---

## 색상 (FluentColors)

| 이름 | 현재 값 | 용도 |
|------|---------|------|
| Background | `#F5F7FA` | 앱 전체 배경 |
| Surface | `#FFFFFF` | 카드, 입력필드 배경 |
| Accent | `#0070F0` | 주요 버튼, 선택된 항목, 포커스 테두리 |
| AccentHover | `#005AC8` | Accent 호버 상태 |
| AccentLight | `#EBF4FF` | Accent 연한 배경 |
| NavBg | `#1E1E28` | 사이드바 배경 |
| NavHover | `#2D2D3A` | 사이드바 항목 호버 |
| NavSelected | `#0070F0` | 사이드바 선택된 항목 배경 |
| TextPrimary | `#14141E` | 기본 텍스트 |
| TextSecond | `#646E82` | 보조 텍스트, 레이블 |
| TextOnDark | `#DCE1EB` | 어두운 배경 위 텍스트 (사이드바) |
| Divider | `#E1E4EB` | 구분선 |
| CardBorder | `#DCE0E8` | 카드 테두리 |
| Success | `#10A864` | 완료 상태 |
| Warning | `#F0A000` | 진행중 상태 |
| Danger | `#DC3232` | 오류, 위험 버튼 |
| ProgressBg | `#DCE4F0` | 진행률 바 배경 |

---

## 폰트 (FluentFonts)

| 이름 | 현재 값 | 용도 |
|------|---------|------|
| Body | `Segoe UI 9.5pt` | 기본 텍스트, 입력필드 |
| BodyBold | `Segoe UI 9.5pt Bold` | 강조 텍스트 |
| Caption | `Segoe UI 8.5pt` | 작은 레이블 |
| Title | `Segoe UI 13pt` | 제목 |
| TitleBold | `Segoe UI 13pt Bold` | 굵은 제목 |
| NavItem | `Segoe UI 9.5pt` | 사이드바 메뉴 항목 |
| Small | `Segoe UI 8pt` | 뱃지 텍스트 |
| Header | `Segoe UI 11pt Bold` | 섹션 헤더 |

> 폰트 패밀리를 바꾸려면 여기에 원하는 폰트명을 적어주세요.
> 예: `Pretendard`, `Malgun Gothic`, `Noto Sans KR`

---

## 컴포넌트 스타일

### CardPanel (카드)
- 배경: `Surface (#FFFFFF)`
- 테두리: `CardBorder (#DCE0E8)`, 1px
- 모서리 반경: `10px`
- 패딩: `16px`

### FluentButton (버튼)
- 높이: `34px`
- 모서리 반경: `6px`
- **Primary**: 배경 `Accent`, 텍스트 흰색
- **Secondary**: 배경 `#F8F9FC`, 텍스트 `TextPrimary`, 테두리 `CardBorder`
- **Danger**: 배경 `#FFF5F5`, 텍스트 `Danger`, 테두리 `#F0C8C8`

### RoundTextBox (입력 필드)
- 높이: `42px`
- 모서리 반경: `10px`
- 테두리 평상시: `#D9D9D9`, 1px
- 테두리 포커스: `#0078D4 (Accent)`, 2px
- 배경: `#FFFFFF`
- 폰트: `Segoe UI 9.5pt`
- 내부 좌우 패딩: `8px`

### RoundComboBox (드롭다운)
- RoundTextBox와 동일한 스타일
- 내부 좌우 패딩: `6px`

### ProgressBar2 (진행률 바)
- 높이: `6px`
- 모서리 반경: `3px`
- 배경: `ProgressBg (#DCE4F0)`
- 채움: `Accent (#0070F0)`

### StatusBadge (상태 뱃지)
- 크기: `52 × 22px`
- 모서리 반경: `10px` (pill 형태)
- **완료**: 배경 `#E6FAF0`, 텍스트 `Success (#10A864)`
- **진행중**: 배경 `#FFF8E6`, 텍스트 `Warning (#F0A000)`
- **대기**: 배경 `#F0F2F8`, 텍스트 `TextSecond (#646E82)`

### NavItem (사이드바 메뉴)
- 높이: `40px`
- 선택됨: 배경 `NavSelected`, 왼쪽 3px 강조 바
- 배경: `NavBg (#1E1E28)`
- 텍스트: `TextOnDark (#DCE1EB)`

---

## 변경하려면

원하는 값으로 이 파일을 수정한 뒤 저장하고 알려주세요.
적용할게요.
