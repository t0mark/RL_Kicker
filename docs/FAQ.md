# 자주 묻는 질문

## 실행/설치

**Q. `mlagents-learn` 실행 시 패키지 버전 경고가 납니다.**  
A. `pip show mlagents`와 `pip show mlagents-envs` 버전이 다르면 제거 후 동일 버전을 설치하세요. 로컬 패키지를 편집 중이라면 `pip install -e ./ml-agents` 형태로 설치합니다.

**Q. Unity 에디터에서 `TensorFlowSharp` 관련 오류가 뜹니다.**  
A. 이 저장소는 PyTorch 기반이라 무시해도 되며, 패키지를 다시 임포트하면 사라집니다.

## 학습 품질

**Q. 보상이 음수로만 나오거나 학습이 발산합니다.**  
A. `Training-Configuration-File.md`에서 `learning_rate`를 줄이고, 보상 스케일을 0.1~1.0 사이로 제한하세요. 관측 값이 너무 커지면 `Mathf.Clamp`로 정규화합니다.

**Q. 에이전트가 전혀 움직이지 않습니다.**  
A. `Behavior Parameters`의 `Behavior Type`이 `Default`인지 확인하고, `Heuristic` 구현이 비어 있으면 기본값 0이 들어갑니다. Dummy 값을 채우거나 `Decision Requester`를 활성화하세요.

**Q. Self-Play가 시작되지 않습니다.**  
A. Unity 측에서 `Self Play` 섹션을 켜고, YAML에 동일한 `self_play` 구성이 있는지 확인하세요. `keep_checkpoints`가 0이면 과거 모델을 찾지 못합니다.

## 빌드/배포

**Q. Docker에서 X 서버 관련 오류가 납니다.**  
A. Headless 모드(`-nographics -batchmode`)로 빌드를 만들고 `Using-Docker.md`의 예시처럼 `DISPLAY` 변수를 비워두세요.

**Q. 결과 파일을 Git으로 관리할 수 있을까요?**  
A. 체크포인트는 대용량이므로 Git LFS를 쓰거나, 필요한 `.onnx`만 `Assets/ML-Agents/Models` 같은 폴더에 복사해 버전 관리하는 것이 좋습니다.
