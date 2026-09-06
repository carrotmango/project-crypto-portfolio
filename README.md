#  Project Crypto: Trading Simulator

![Project Crypto Banner](./banner.png)

> **90년대 레트로 픽셀 미학과 현대 가상화폐 시장의 변동성을 결합한 금융 서바이벌 시뮬레이션**   
> C# & Unity 기반 1인 개발 프로젝트 (WebGL 피드백 테스트 및 Steam 데모 출시 완료) 

[![Steam Store](https://img.shields.io/badge/Steam-Store_Demo-blue?logo=steam)](https://store.steampowered.com/app/4577680/Project_Crypto_Trading_Simulator_Demo/)

---

##  1. Project Overview (프로젝트 개요)

* **게임 타이틀**: Project Crypto Trading Simulator 
* **장르**: 레트로 픽셀 트레이딩 시뮬레이션 
* **플랫폼**: WebGL (`carrotmango.xyz` 피드백 수집용) / PC (Steam 데모 배포) 
* **개발 환경**: Unity (6000.2.7f2), C# 
* **게임 목표**: 
  * **유저**: 초기 시드머니로 시작해 실시간 시장 흐름을 분석하고, 자산 증식 및 회사 자본 납입을 통해 추가 기능을 해금하며 최고의 부를 달성.
  * **개발자**: WebGL 테스트 배포를 통한 버그 수정 및 유저 확보 후 볼륨 있는 콘텐츠를 포함한 Steam 정식 출시.

---

##  2. Core System Architecture (핵심 시스템 디자인)

### ① 동적 가격 결정 엔진 (Price Engine)
* **Market Phase 시스템**: 전역 시장 상태를 `MegaBull`부터 `MegaBear`까지 **11단계**로 세분화하여 상승/하락 확률(`directionBias`) 및 변동 폭(`baseMagnitude`)을 동적으로 적용 .
* **자산별 변동성 (Volatility Level)**: 메이저 코인부터 밈(Meme) 코인까지 1~5단계 변동성 레벨을 부여해 하이리스크-하이리턴 구조 설계 .
* **스테이블 코인 디페깅**: USDT, USDC 등에 실제 환율 데이터 연동 및 미세 노이즈(`depeggingNoise`) 산출 로직 구현 .
* **실매매 알고리즘**: 데이터 부하 방지를 위한 최소 주문금액(5,000원) 제약, 수수료 산입 평단가 실시간 UI 동기화, 차트 내 매수(B)/매도(S) 실시간 마킹 .

### ② 하이리스크 파생상품 엔진 (Perpetual & Leverage)
* **다중 증거금 모드**: 전재산을 담보로 청산가를 재계산하는 **교차(Cross)** 및 투입 증거금 내 리스크를 제한하는 **격리(Isolate)** 마진 지원 .
* **직급 기반 레버리지 해금**: 유저 직급에 따라 최대 레버리지를 차등 제한(사원 5x ~ 차장 20x ~ 최상위 100x) .
* **실시간 Net PnL & 강제 청산**: 예상 수수료를 즉시 차감한 실질 수익률 노출 및 청산가 도달 시 긴급 통지 연출 .

### ③ 실시간 이벤트 & 정보 흐름 (Event Orchestration)
* **NewsPanel & 찌라시**: 시나리오 및 조건에 따라 상폐/펌핑/뉴스 포스트를 자동 발행하며, 확률 기반 여론 생성 및 쿨타임 시스템 적용 .
* **Global Notification & SMS**: 급여 입금, 청산 알림, 주요 뉴스를 SMS 알림 형태로 발송해 몰입감 극대화 .

### ④ 금융 인프라 및 고정 수익원 (Banking & Real Estate)
* **은행/대출(Loan System)**: 신용 등급 기반 대출 한도/이자율 설정 및 매일 자정 복리 이자 차감 .
* **부동산 매매**: 원룸, 빌라, 아파트, 빌딩 매입을 통해 하락장에서도 견디는 영구적 임대 수익(Cash Flow) 확보 .
* **일일 정산 엔진**: 매일 자정 급여, 임대 수익, 대출 이자를 통합 계산해 일일 재무 리포트 제공 .

### ⑤ 외출, 아르바이트 & Dapp 시스템
* **아르바이트 미니게임**: 물류 분류 미니게임(빠른 지원 / 심화 지원)을 통해 성과 기반 초기 시드머니 확보 .
* **다중 Dapp 지원**: 일반 코인 거래소(BullBit) 외 DEX(밈코인), NFT 거래소, 크립토 지갑, 정보 수집용 술집 구현 .

---

## 3. Graphics & Art Style

* **비주얼 컨셉**: 90년대 PC-98 플랫폼 특유의 8-bit / 16-bit 레트로 픽셀 아트 지향 .
* **UI/UX Design**: 레트로 아이콘과 텍스트를 유지하면서 실시간 차트 및 데이터 패널은 현대적 가독성을 확보 .

---

##  4. Development Roadmap & Milestones (2026)

* **Phase 1 (2026.02)**: 핵심 루프 고도화, 선물 거래 및 부동산 시스템 최종 밸런싱 
* **Phase 2 (2026.03 ~ 2026.04)**: Steam 상점 페이지 및 홍보용 트레일러/데모 등록, WebGL 유저 피드백 수집 
* **Phase 3 (~2027)**: DEX, NFT 거래소 등 엔드 콘텐츠 추가, 사운드/그래픽 폴리싱 후 Steam 정식 출시 
