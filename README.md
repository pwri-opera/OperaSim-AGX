# ConstSim
## ソフトウェア要件
- Unity：2020.3.11f1
- AGX：2.29.4.4(x64, VS2015)
- Ubuntu：18.04
- ROS：Melodic
## 実行方法
### ROS側(初期設定)
- rosbridge_websocketの起動
   - `roslaunch rosbridge_server rosbridge_websocket.launch`
- ショベルの初期姿勢の設定
   - ROSメッセージを介して，Unityモデルの関節角度の入力を行う
   - cmdメッセージを送信し、モデルの初期姿勢を定義する。以下に実行例を示す
      - 以下を実行することで，初期姿勢が定義される  
         - `rostopic pub /zx120/boom/cmd std_msgs/Float64 "data: -0.8"` 
         - `rostopic pub /zx120/bucket/cmd std_msgs/Float64 "data: 1.0"`
         - `rostopic pub /zx120/arm/cmd std_msgs/Float64 "data: 2"`
### Unity側
- Unity Hubより，本プロジェクトファイル(ConstSim)を追加
- 追加されたプロジェクトのタイトルを押し，Unity Editorで開く
- 建設機械のモデルが表示されていない場合には，プロジェクトウィンドウからAssets/Scenes/MainScene.unityをダブルクリックしてロードする
- Assets/AGXUnity/Plugins/x86_64のフォルダへAGX Dynamicsのライセンスファイルをコピー&ペースト
- HierarchyウィンドウからRosConnectorというGameObjectを選択し，Ros Bridge Sercer Urlを入力する
- Playボタンを押してシミュレーションを実行
### ROS側(制御プログラムの実行)
- Moveitに基づく油圧ショベルを行うためには以下を実行
   - `roslaunch zx120_unity zx120_standby.launch`
   - マーカを任意の位置に移動させ，`Plan&Execute`ボタンを押すと，Unity側のモデルの制御が行われる．
- move_baseに基づくクローラダンプの制御を行うためには，以下を実行
   - `roslaunch ic120_unity ic120_standby.launch`
   - `2D Nav Goal`を押して任意の位置，方向を指定することで，ダンプトラックが目標に向かって制御される．
