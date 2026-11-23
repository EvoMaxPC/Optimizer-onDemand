#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod engine;

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            engine::engine_hello,
            engine::engine_version,
            engine::system_os_info,
            engine::catalog_list,
            engine::action_apply,
            engine::action_rollback,
            engine::snapshot_list
        ])
        .run(tauri::generate_context!())
        .expect("error while running EVO desktop shell");
}
