# ML-Agents 훈련 워크플로

강화학습 루프는 크게 *환경 준비 → YAML 설정 → 학습 실행 → 체크포인트 관리 → 추론 전환* 순서로 진행됩니다.

## 1. 환경 및 패키지 준비

1. Unity 프로젝트에서 `ML Agents` 패키지가 올바르게 참조되는지 확인합니다. (이 저장소에서는 `Packages/manifest.json`에서 로컬 패키지를 가리키고 있습니다.)
2. Python 측에서는 `mlagents`와 `mlagents-envs`가 같은 버전으로 설치되어야 합니다. 가상 환경을 사용해 충돌을 막습니다.
3. 학습 로그를 남길 `results/<run-id>` 디렉터리를 미리 비워두면 실험 비교가 쉽습니다.

## 2. YAML 설정 개요

`config/` 아래 YAML은 `run-id`와 동일한 이름으로 참조됩니다. 핵심 필드:

- `max_steps`: 한 학습 실험에서 총 스텝 수. 작은 값으로 빠르게 수렴 여부를 확인한 뒤 늘립니다.
- `time_horizon`: GAE 누적 길이. 물리 환경은 64~128이 안정적입니다.
- `summary_freq`: TensorBoard 기록 주기. 너무 작으면 I/O가 증가합니다.
- `keep_checkpoints`: 저장할 체크포인트 개수. 디스크 용량과 복구 필요성 사이에서 조절합니다.

세부 파라미터 설명은 `Training-Configuration-File.md`를 참고하세요.

## 3. 학습 실행

```bash
mlagents-learn config/soccer-ppo.yaml --run-id=soccer-v0 --env=Builds/Soccer/Soccer.x86_64 --no-graphics
```

- 에디터에서 바로 실행하려면 `--env`를 생략하고 Unity 플레이 버튼을 누릅니다.
- 여러 환경을 병렬로 돌리고 싶다면 `--num-envs <N>`(Python side) 혹은 `--env`에 `--num-envs` 지원 빌드를 연결합니다.
- `--resume` 플래그로 중단된 학습을 이어갈 수 있습니다.

## 4. 체크포인트와 추론

- 기본 체크포인트 위치: `results/<run-id>/<behavior_name>/<timestamped_model>.onnx`.
- Unity 씬에서 `Behavior Parameters` > `Model` 슬롯에 `.onnx` 파일을 넣고 `Behavior Type`을 `Inference Only`로 두면 추론 모드로 전환됩니다.
- 훈련된 모델을 버전 관리하려면 `results/`에서 필요한 폴더만 보관하고 나머지는 정리합니다.

## 5. 모니터링

- `tensorboard --logdir results` 로 학습 곡선을 확인합니다.
- 학습 도중 정책이 발산하면 `Training-PPO.md`의 하이퍼파라미터 가이드를 참고해 학습률이나 클립 값을 조정하세요.
- Docker나 원격 서버에서 돌릴 때는 `Using-Docker.md`의 포트/볼륨 설정을 따라 로그와 모델을 가져옵니다.

간단한 루틴만 정리한 문서이므로, 세부 API는 Unity 공식 참조를 확인하세요.
