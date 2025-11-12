# 유니티 ML-Agents 핵심 가이드

이 문서는 Soccer 기반 강화학습 프로젝트에 필요한 최소한의 정보만 한국어로 정리한 버전입니다. 전체 레퍼런스는 [Unity 공식 패키지 문서](https://docs.unity3d.com/Packages/com.unity.ml-agents@latest)를 참고하세요.

## 문서 구성

- `Installation.md` / `Installation-Anaconda-Windows.md`: 플랫폼별 요구 사항과 의존성 설치 단계.
- `Using-Docker.md`: Docker로 훈련 환경을 띄울 때 필요한 볼륨/포트 구성.
- `Learning-Environment-Guide.md`: 에이전트, 행동, 보상 디자인 핵심만 요약.
- `Training-ML-Agents.md`: 훈련 워크플로(학습 실행, 체크포인트, 추론 모드 전환).
- `Training-Configuration-File.md`: YAML 설정 주요 섹션과 자주 쓰는 하이퍼파라미터 큐시트.
- `Training-PPO.md` / `Training-Imitation-Learning.md`: 알고리즘별 옵션과 팁.
- `FAQ.md`: 빌드/학습 중 자주 생기는 문제 해결.

필요한 문서만 남아 있으므로, 다른 주제(예: Gym 연동, API Reference 등)는 상단 공식 링크를 통해 원문을 찾아보면 됩니다.

## 빠른 시작

1. **엔진/도구 설치:** `Installation.md` 를 따라 Unity, Python 환경을 맞춥니다.
2. **환경 구성:** Soccer 씬을 복제하거나 `Learning-Environment-Guide.md` 를 참고해 새 Agent를 설계합니다.
3. **훈련 설정:** `Training-Configuration-File.md` 를 참고해 YAML을 정리합니다.
4. **훈련 실행:** `Training-ML-Agents.md` 의 절차대로 `mlagents-learn` 을 실행하고, 필요 시 `Training-PPO.md` 파라미터를 조정합니다.
5. **배포/문제 해결:** Docker 사용 시 `Using-Docker.md`, 에러는 `FAQ.md` 를 확인하세요.

문서에 없는 세부 항목이 필요하면 Git 이력의 `docs_en_backup/` 또는 Unity 최신 문서를 참조할 수 있습니다.
