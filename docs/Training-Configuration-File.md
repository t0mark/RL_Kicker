# 학습 설정(YAML) 요약

`config/` 폴더의 YAML 파일은 Behavior 단위로 하이퍼파라미터를 정의합니다. 아래 표는 Soccer 시나리오에서 자주 수정하는 항목만 추려 설명합니다.

| 경로 | 설명 | 권장 범위 |
| --- | --- | --- |
| `behaviors.<name>.trainer_type` | `ppo`, `sac` 등 트레이너 선택 | PPO가 기본 |
| `hyperparameters.batch_size` | 한 번의 업데이트에 사용되는 스텝 수 | 1024~4096 |
| `hyperparameters.buffer_size` | 정책 업데이트 전 모으는 전체 스텝 | `batch_size`의 10배 이상 |
| `hyperparameters.learning_rate` | Adam 학습률 | 1e-5 ~ 3e-4 |
| `hyperparameters.beta` | 엔트로피 보너스. 탐험 정도 | 1e-3 이하 |
| `hyperparameters.epsilon` | PPO 클리핑 한계 | 0.1 ~ 0.3 |
| `network_settings.hidden_units` | MLP 중간 노드 수 | 128~512 |
| `network_settings.num_layers` | MLP 층 수 | 2~3 |
| `reward_signals.extrinsic.gamma` | 할인율 | 0.95~0.995 |
| `reward_signals.extrinsic.strength` | Extrinsic 보상 가중치 | 1.0 (기본) |
| `self_play.window` | Self-Play 히스토리 길이 | 5~20 |
| `max_steps` | 총 학습 스텝 | 5e5 이상 |
| `time_horizon` | GAE 누적 길이 | 64~128 |
| `summary_freq` | TensorBoard 업데이트 주기 | 1000~5000 |

## 커스터마이징 절차

1. **Behavior명 정렬**: Unity `Behavior Parameters`의 이름과 YAML의 `behaviors` 키가 일치해야 합니다.
2. **관측/행동 크기 반영**: `network_settings`는 관측 차원과 직접적인 관계는 없지만, 관측 수가 늘어나면 `hidden_units`를 늘리는 편이 안정적입니다.
3. **Self-Play**: `self_play` 블록을 추가하면 자동으로 과거 정책을 적으로 사용합니다. `save_steps`를 너무 작게 두면 디스크 사용량이 커집니다.
4. **체크포인트 설정**: `checkpoint_interval`은 SAC에서만 사용됩니다. PPO에서는 `keep_checkpoints`만 조절하면 됩니다.

## 팁

- `batch_size * num_envs`가 GPU 메모리를 초과하면 Python 프로세스가 종료됩니다. 에러가 나면 `batch_size`를 우선 줄이세요.
- 관측이 많은 환경일수록 `learning_rate`와 `beta`를 동시에 낮추면 진동이 줄어듭니다.
- YAML을 바꿀 때마다 `run-id`를 새로 지정하면 이전 로그가 덮어쓰이지 않습니다.
