# 학습 환경 설계 가이드

Soccer 에이전트를 포함해 대부분의 ML-Agents 환경은 `Behavior Parameters`, `Decision Requester`, `Agent` 스크립트의 세 가지 축을 튜닝하는 작업으로 정리할 수 있습니다. 아래 체크리스트만 충족하면 대부분의 강화학습 문제가 수렴합니다.

## 1. 씬 구성

1. **관측(Observation)**  
   - Vector 관측은 `Agent.CollectObservations()`에서 `AddObservation` 호출을 통해 정의합니다.  
   - 시각 정보가 필요할 때는 `CameraSensorComponent` 나 `RenderTexture` 기반 `CameraSensor`를 붙입니다.  
   - 관측 크기 변경 시 `BehaviorParameters`의 `Observation Specs`와 YAML `vector_observation_size`가 일치해야 합니다.

2. **행동(Action)**  
   - 연속 제어: `Behavior Type`을 `Default`(연속)으로 두고 `Action Spec`에서 `Continuous` 사이즈를 지정합니다.  
   - 이산 제어: 분기(branch)당 선택지 수를 정의하고, Unity Inspector에서 `Branch Size`를 설정합니다.  
   - 혼합 액션이 필요하면 연속/이산을 모두 지정할 수 있지만, Soccer류 환경은 보통 연속 제어만으로 충분합니다.

3. **보상(Reward)**  
   - 즉시 보상은 `AddReward`, 에피소드 종료 시 `EndEpisode`와 함께 `SetReward`를 병행합니다.  
   - Sparse 보상만 사용하면 학습이 느리므로, 공을 맞췄을 때 +0.1 등 미세한 shaping을 추가합니다.  
   - Self-Play 사용 시 팀 간 밸런스를 위해 상대 보상을 `-reward` 형태로 주입하지 말고, 상대 Agent가 직접 보상을 받도록 설계합니다.

## 2. 에피소드와 타이밍

- `Decision Requester`의 `Decision Period`를 1로 두면 매 프레임 행동을 선택합니다. 물리 기반 환경이라면 5~10 프레임마다 의사결정을 내려도 안정적입니다.
- `Max Step`은 한 에피소드에서 허용할 프레임 수입니다. 학습 곡선을 보기 쉽도록 골 득점/리셋이 자주 일어나지 않는다면 5000 프레임 이하로 제한하세요.
- 에피소드가 자연스럽게 끝나지 않는 환경이라면, 시간 초과 시 `Agent.EndEpisode()`를 호출해 누적 리워드를 초기화합니다.

## 3. 멀티 에이전트

- 서로 다른 동작을 학습시키고 싶으면 Behavior 이름을 분리합니다 (`Offense`, `Goalie` 등).  
- 동일한 정책을 공유해야 한다면 Behavior를 동일하게 두고 Agent 수만 늘립니다. 학습 속도를 높이려면 인스턴스를 병렬로 배치합니다.
- Self-Play는 `BehaviorParameters` > `Self Play` 를 활성화하고, YAML에서 `self_play` 블록을 설정하면 됩니다.

## 4. 디버깅 팁

- `Heuristic()` 메서드를 구현하면 키보드/패드 입력으로 행동을 덮어쓸 수 있어 관측·보상을 빠르게 검증할 수 있습니다.
- `Behavior Parameters` > `Inference Device`를 `CPU`로 고정한 뒤 에디터에서 바로 학습 정책을 평가할 수 있습니다.
- 학습 중 값 폭주가 보이면, 관측 값을 `Mathf.Clamp`나 정규화로 제한하고, 보상 스케일을 1 미만으로 줄이세요.

필요 시 구체적인 API 설명은 Unity 공식 문서에서 확인하고, 여기서는 프로젝트에 바로 적용할 최소 설정만 유지합니다.
